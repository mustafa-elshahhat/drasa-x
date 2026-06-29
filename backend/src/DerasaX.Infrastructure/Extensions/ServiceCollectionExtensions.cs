
using System;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Domain.Interfaces;
using DerasaX.Infrastructure.DbHelper.Context;
using DerasaX.Infrastructure.Interceptors;
using DerasaX.Infrastructure.Repositories;
using DerasaX.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DerasaX.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContextServices(configuration);
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            // Phase 5 — platform-owned entity repository (shares the scoped DbContext with the UoW).
            services.AddScoped(typeof(IPlatformRepository<>), typeof(PlatformRepository<>));

            // Phase 16 — durable file storage providers + tenant-filter-bypassing lookup.
            services.AddFileStorageProviders(configuration);

            // Phase 19 — file-retention/purge maintenance + its interval-driven host (disabled by default).
            services.AddScoped<DerasaX.Application.Services.Abstractions.Storage.IFileRetentionService,
                DerasaX.Infrastructure.Storage.FileRetentionService>();
            services.AddHostedService<DerasaX.Infrastructure.Storage.FileRetentionBackgroundService>();

            return services;
        }

        /// <summary>
        /// Phase 16 — binds the <c>FileStorage</c> options and registers the binary providers.
        /// The active <see cref="IFileStorageProvider"/> is selected by <c>FileStorage:Provider</c>
        /// ("Local" default, "S3" for production object storage).
        /// </summary>
        private static void AddFileStorageProviders(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FileStorageSettings>(configuration.GetSection(FileStorageSettings.SectionName));

            services.AddSingleton<LocalFileStorageProvider>();
            services.AddSingleton<S3FileStorageProvider>();
            services.AddSingleton<IFileStorageProvider>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<FileStorageSettings>>().Value;
                return settings.Provider.Equals("S3", StringComparison.OrdinalIgnoreCase)
                    ? sp.GetRequiredService<S3FileStorageProvider>()
                    : sp.GetRequiredService<LocalFileStorageProvider>();
            });

            services.AddScoped<IFileRecordLookup, FileRecordLookup>();

            // Phase 18 — malware-scan policy. Default "Disabled" => uploads recorded NotScanned
            // (honest; preserves prior behaviour). "Stub" enables the deterministic EICAR test
            // scanner; "Unavailable" models an engine that cannot produce a verdict. A real
            // ClamAV/cloud scanner would be added as a new mode for staging/production.
            services.AddSingleton<IFileScanner>(sp =>
                FileScannerFactory.Create(sp.GetRequiredService<IOptions<FileStorageSettings>>()));
        }

        private static void AddDbContextServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Phase 13 — a per-scope interceptor that pushes every newly-inserted notification to its
            // recipient's SignalR group after commit (real-time, best-effort). Registered as scoped and
            // resolved per DbContext so its captured-pending list is request-local.
            services.AddScoped<NotificationRealtimeInterceptor>();
            services.AddDbContext<DerasaXDbContext>((sp, options) =>
                options.UseNpgsql(configuration.GetConnectionString("cs"))
                       .AddInterceptors(sp.GetRequiredService<NotificationRealtimeInterceptor>()));
        }
    }
}
