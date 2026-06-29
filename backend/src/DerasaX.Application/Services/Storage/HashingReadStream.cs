using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DerasaX.Application.Services.Storage
{
    /// <summary>
    /// Phase 16 — a read-through stream that computes a streaming SHA-256 and counts bytes as the
    /// storage provider reads the source. Avoids buffering the whole (up to 100 MB) file in memory
    /// or reading it twice: the hash and authoritative byte count are available once the provider
    /// has consumed the stream and <see cref="Finish"/> is called.
    /// </summary>
    public sealed class HashingReadStream : Stream
    {
        private readonly Stream _inner;
        private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        private long _bytesRead;
        private byte[]? _final;

        public HashingReadStream(Stream inner) => _inner = inner;

        public long BytesRead => _bytesRead;

        /// <summary>Finalizes and returns the lowercase hex SHA-256 of all bytes read.</summary>
        public string Finish()
        {
            _final ??= _hash.GetHashAndReset();
            return Convert.ToHexString(_final).ToLowerInvariant();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = _inner.Read(buffer, offset, count);
            if (n > 0) { _hash.AppendData(buffer, offset, n); _bytesRead += n; }
            return n;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            var n = await _inner.ReadAsync(buffer, ct);
            if (n > 0) { _hash.AppendData(buffer.Span[..n]); _bytesRead += n; }
            return n;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            var n = await _inner.ReadAsync(buffer.AsMemory(offset, count), ct);
            if (n > 0) { _hash.AppendData(buffer, offset, n); _bytesRead += n; }
            return n;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _inner.CanSeek ? _inner.Length : _bytesRead;
        public override long Position { get => _bytesRead; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) _hash.Dispose();
            base.Dispose(disposing);
        }
    }
}
