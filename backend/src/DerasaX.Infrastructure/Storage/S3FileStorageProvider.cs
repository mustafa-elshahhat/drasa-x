using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Domain.Exceptions;
using Microsoft.Extensions.Options;
using System.Net;

namespace DerasaX.Infrastructure.Storage
{
    /// <summary>
    /// Phase 16 — production S3-compatible durable provider (AWS S3, MinIO, Cloudflare R2, Spaces).
    /// All connection details come from configuration/environment — never hardcoded. When the
    /// provider is selected but not configured with a bucket + credentials (the local/CI reality),
    /// every operation fails honestly with <see cref="StorageUnavailableException"/>; it never fakes
    /// a successful upload/download. A real round-trip requires real credentials + a reachable bucket.
    /// </summary>
    public sealed class S3FileStorageProvider : IFileStorageProvider
    {
        private readonly S3StorageOptions _opts;
        private readonly Lazy<IAmazonS3> _client;

        public S3FileStorageProvider(IOptions<FileStorageSettings> settings)
        {
            _opts = settings.Value.S3;
            _client = new Lazy<IAmazonS3>(BuildClient);
        }

        public string Name => "S3";

        public async Task SaveAsync(string storageKey, Stream content, string contentType, CancellationToken ct = default)
        {
            var client = Client();
            try
            {
                var req = new PutObjectRequest
                {
                    BucketName = _opts.Bucket,
                    Key = storageKey,
                    InputStream = content,
                    ContentType = contentType,
                    AutoCloseStream = false
                };
                await client.PutObjectAsync(req, ct);
            }
            catch (AmazonS3Exception ex)
            {
                throw new StorageUnavailableException($"S3 upload failed: {ex.ErrorCode}.", ex);
            }
        }

        public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
        {
            var client = Client();
            try
            {
                var resp = await client.GetObjectAsync(new GetObjectRequest { BucketName = _opts.Bucket, Key = storageKey }, ct);
                return resp.ResponseStream;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
            {
                throw new NotFoundException("Stored file content not found.");
            }
            catch (AmazonS3Exception ex)
            {
                throw new StorageUnavailableException($"S3 read failed: {ex.ErrorCode}.", ex);
            }
        }

        public async Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
        {
            var client = Client();
            try
            {
                await client.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = _opts.Bucket, Key = storageKey }, ct);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
            {
                return false;
            }
            catch (AmazonS3Exception ex)
            {
                throw new StorageUnavailableException($"S3 metadata lookup failed: {ex.ErrorCode}.", ex);
            }
        }

        public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
        {
            var client = Client();
            try
            {
                await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _opts.Bucket, Key = storageKey }, ct);
            }
            catch (AmazonS3Exception ex)
            {
                throw new StorageUnavailableException($"S3 delete failed: {ex.ErrorCode}.", ex);
            }
        }

        private IAmazonS3 Client()
        {
            if (!_opts.IsConfigured)
                throw new StorageUnavailableException(
                    "S3 storage is selected but not configured. Set FileStorage:S3 Bucket, AccessKeyId and SecretAccessKey " +
                    "(via environment/secret store) to enable durable object storage.");
            return _client.Value;
        }

        private IAmazonS3 BuildClient()
        {
            var config = new AmazonS3Config { ForcePathStyle = _opts.ForcePathStyle };
            if (!string.IsNullOrWhiteSpace(_opts.ServiceUrl))
                config.ServiceURL = _opts.ServiceUrl;
            else if (!string.IsNullOrWhiteSpace(_opts.Region))
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(_opts.Region);

            var creds = new BasicAWSCredentials(_opts.AccessKeyId, _opts.SecretAccessKey);
            return new AmazonS3Client(creds, config);
        }
    }
}
