using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

// Spins up the real API pipeline (routing, auth, controllers) in memory,
// swapping the SQL Server DbContext for a fresh EF Core InMemory database
// per factory instance, and Azure Blob Storage for a fake, so tests never
// touch a real SQL Server or a real Azure account.
public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private readonly string dbName = $"RaceDayTests_{Guid.NewGuid()}";

    /// <summary>The fake that replaces Azure Blob Storage for this factory.</summary>
    public FakeBlobStorageService Storage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove EVERY registration that carries the SQL Server configuration for RaceDayContext.
            // Newer EF Core versions also register IDbContextOptionsConfiguration<RaceDayContext>,
            // so removing only DbContextOptions<RaceDayContext> leaves SQL Server in place.
            var toRemove = services
                .Where(d => d.ServiceType == typeof(RaceDayContext)
                         || d.ServiceType == typeof(DbContextOptions<RaceDayContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GenericTypeArguments.Contains(typeof(RaceDayContext))))
                .ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<RaceDayContext>(options =>
                options.UseInMemoryDatabase(this.dbName));

            // Azure Blob Storage -> fake
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService>(this.Storage);
        });
    }
}
