using DataPlatform.Api.Data;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace DataPlatform.Api.Tests.Common;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;

    public CustomWebApplicationFactory()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();
    }

    // ----------------------------------------------------
    // Testcontainers startup
    // ----------------------------------------------------
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;

        await using var db = new ApplicationContext(options);
        await db.Database.MigrateAsync();
    }

    // ----------------------------------------------------
    // ASP.NET host configuration
    // ----------------------------------------------------
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // -------------------------------
            // Replace EF Core DbContext
            // -------------------------------
            var dbDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationContext>));

            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            services.AddDbContext<ApplicationContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));

            // -------------------------------
            // Remove existing MassTransit setup
            // -------------------------------
            services.RemoveAll<IBus>();
            services.RemoveAll<IBusControl>();
            services.RemoveAll<IPublishEndpoint>();
            services.RemoveAll<ISendEndpointProvider>();

            // -------------------------------
            // MassTransit + RabbitMQ (Testcontainers)
            // -------------------------------
            services.AddMassTransitTestHarness(x =>
            {
                // Register all consumers from API assembly
                x.AddConsumers(typeof(Program).Assembly);

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(
                        _rabbitMqContainer.Hostname,
                        _rabbitMqContainer.GetMappedPublicPort(5672),
                        "/",
                        h =>
                        {
                            h.Username("guest");
                            h.Password("guest");
                        });

                    // Automatically configure queues/exchanges
                    cfg.ConfigureEndpoints(context);
                });
            });
        });
    }

    // ----------------------------------------------------
    // Testcontainers teardown
    // ----------------------------------------------------
    public new async Task DisposeAsync()
    {
        await _rabbitMqContainer.StopAsync();
        await _dbContainer.StopAsync();
    }

    // ----------------------------------------------------
    // Helpers (optional but useful)
    // ----------------------------------------------------
    public ApplicationContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;

        return new ApplicationContext(options);
    }

    public static async Task ClearDatabaseAsync(ApplicationContext db)
    {
        var tableNames = await db.Database
            .SqlQueryRaw<string>(@"
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name <> '__EFMigrationsHistory'
                  AND table_type = 'BASE TABLE';
            ")
            .ToListAsync();

        foreach (var table in tableNames)
        {
            await db.Database.ExecuteSqlRawAsync(
                $"TRUNCATE TABLE \"{table}\" RESTART IDENTITY CASCADE;");
        }
    }
}
