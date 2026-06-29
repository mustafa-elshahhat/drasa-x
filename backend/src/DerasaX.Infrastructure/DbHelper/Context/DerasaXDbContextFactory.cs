using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DerasaX.Infrastructure.DbHelper.Context
{
    /// <summary>
    /// Design-time factory so <c>dotnet ef</c> can build the context without running
    /// the web host. The tenant context is null at design time (no query-filter
    /// evaluation occurs during migration generation). The connection string comes
    /// from <c>DERASAX_DESIGN_CS</c> or falls back to the local development database.
    /// </summary>
    public class DerasaXDbContextFactory : IDesignTimeDbContextFactory<DerasaXDbContext>
    {
        public DerasaXDbContext CreateDbContext(string[] args)
        {
            var cs = Environment.GetEnvironmentVariable("DERASAX_DESIGN_CS")
                ?? "Host=localhost;Port=5432;Database=derasax_local;Username=derasax;Password=derasax_local_dev;SSL Mode=Disable;Trust Server Certificate=true";

            var options = new DbContextOptionsBuilder<DerasaXDbContext>()
                .UseNpgsql(cs)
                .Options;

            return new DerasaXDbContext(options, tenant: null);
        }
    }
}
