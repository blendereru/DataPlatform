using System.Reflection;
using DataPlatform.Api.Consumers;
using DataPlatform.Api.Data;
using DataPlatform.Api.Hubs;
using DataPlatform.Api.Services;
using DataPlatform.Api.Services.Abstractions;
using Hangfire;
using Hangfire.PostgreSql;
using MassTransit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, loggerConfig) => 
{
    loggerConfig.ReadFrom.Configuration(context.Configuration);
});

// ============================================================================
// Authentication & Authorization
// ============================================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/auth/signin";
        options.AccessDeniedPath = "/auth/denied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
});

// ============================================================================
// Database
// ============================================================================
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration
    .GetConnectionString("DefaultConnection"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContext<ApplicationContext>(opts =>
{
    opts.UseNpgsql(dataSource);
});

// ============================================================================
// Data Platform Services - NEW!
// ============================================================================
builder.Services.AddScoped<IDataSourceConnectionService, DataSourceConnectionService>();
builder.Services.AddScoped<IQueryExecutionService, QueryExecutionService>();
builder.Services.AddScoped<PipelineSchedulerService>();

// ============================================================================
// MassTransit (Message Queue)
// ============================================================================
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddMassTransit(x =>
    {
        // Register pipeline execution consumer
        x.AddConsumer<PipelineExecutionConsumer>();
        
        x.UsingRabbitMq((context, cfg) =>
        {
            var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";
            var rabbitUser = builder.Configuration["RabbitMq:Username"] ?? "guest";
            var rabbitPass = builder.Configuration["RabbitMq:Password"] ?? "guest";
            
            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username(rabbitUser);
                h.Password(rabbitPass);
            });

            // Configure pipeline execution queue
            cfg.ReceiveEndpoint("pipeline-execution-queue", e =>
            {
                e.ConfigureConsumer<PipelineExecutionConsumer>(context);
            });
        });
    });
}

// ============================================================================
// Hangfire (Background Jobs & Scheduling)
// ============================================================================
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddHangfire(config =>
    {
        config.UsePostgreSqlStorage(options =>
        {
            options.UseExistingNpgsqlConnection(new NpgsqlConnection(builder
                .Configuration.GetConnectionString("DefaultConnection")));
        });
    });
    builder.Services.AddHangfireServer();
}

// ============================================================================
// SignalR (Real-time updates)
// ============================================================================
builder.Services.AddSignalR();

// ============================================================================
// API & Documentation
// ============================================================================
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    
    options.SwaggerDoc("v1", new()
    {
        Title = "Data Platform API",
        Version = "v1",
        Description = @"Modern data platform for connecting, cataloging, transforming, and querying data sources.

**Key Features:**
- Connect to PostgreSQL, MySQL, SQL Server, MongoDB
- Automatic schema discovery
- ETL/ELT pipeline orchestration
- Ad-hoc SQL query execution
- Data quality monitoring
- Real-time execution tracking"
    });
});

builder.Services.AddControllersWithViews();

// ============================================================================
// Application Setup
// ============================================================================
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DataPlatform v1");
    c.RoutePrefix = "swagger"; // Access at /swagger
});

app.UseStaticFiles();
app.UseAuthorization();

// Run database migrations
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
await db.Database.MigrateAsync();

// ============================================================================
// Configure Hangfire Recurring Jobs
// ============================================================================
if (!app.Environment.EnvironmentName.Equals("Testing"))
{
    app.UseHangfireDashboard("/jobs");
    
    // Schedule pipeline checker to run every minute
    RecurringJob.AddOrUpdate<PipelineSchedulerService>(
        "check-scheduled-pipelines",
        service => service.CheckScheduledPipelinesAsync(),
        "* * * * *" // Every minute
    );
}

// ============================================================================
// Routes
// ============================================================================
app.MapControllers();
app.MapHub<DataHub>("/datahub");

// Redirect root to Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

public partial class Program { }