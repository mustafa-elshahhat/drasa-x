using System;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Application.Services.Abstractions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DerasaX.Infrastructure.Storage
{
    /// <summary>
    /// Phase 19 — interval-driven host for the file-retention sweep. DISABLED by default
    /// (<c>FileStorage:Retention:Enabled=false</c>) so it never mutates a shared local/test
    /// database; when disabled it records the disabled posture to the job heartbeat and exits.
    /// When enabled it runs <see cref="IFileRetentionService.RunOnceAsync"/> on a fresh scope
    /// every interval. The sweep logic itself is unit-tested directly, independent of this host.
    /// </summary>
    public sealed class FileRetentionBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly RetentionOptions _options;
        private readonly IBackgroundJobHealth _jobHealth;
        private readonly ILogger<FileRetentionBackgroundService> _logger;

        public FileRetentionBackgroundService(
            IServiceProvider services,
            IOptions<FileStorageSettings> settings,
            IBackgroundJobHealth jobHealth,
            ILogger<FileRetentionBackgroundService> logger)
        {
            _services = services;
            _options = settings.Value.Retention;
            _jobHealth = jobHealth;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _jobHealth.RecordDisabled(FileRetentionService.JobName, "retention background sweep disabled by configuration");
                _logger.LogInformation("file-retention background sweep is disabled (FileStorage:Retention:Enabled=false).");
                return;
            }

            try { await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, _options.InitialDelaySeconds)), stoppingToken); }
            catch (OperationCanceledException) { return; }

            var interval = TimeSpan.FromMinutes(Math.Max(1, _options.ScanIntervalMinutes));
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IFileRetentionService>();
                    await svc.RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    // The service already recorded the failure to the job heartbeat; log + keep looping.
                    _logger.LogError(ex, "file-retention background sweep iteration failed; will retry next interval.");
                }

                try { await Task.Delay(interval, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }
}
