using DataPlatform.Api.Consumers;
using DataPlatform.Api.Data;
using DataPlatform.Api.Hubs;
using Hangfire;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, config) =>
{
    config
        .WriteTo.Console()
        .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day);
});
builder.Services.AddDbContext<ApplicationContext>(opts =>
{
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EventMessageConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ReceiveEndpoint("event-message-queue", e =>
        {
            e.ConfigureConsumer<EventMessageConsumer>(context);
        });
    });
});
builder.Services.AddHangfire(config =>
{
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddHangfireServer();
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen();
builder.Services.AddControllersWithViews();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DataPlatform v1");
});
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.UseHangfireDashboard("/jobs");
app.MapHub<DataHub>("/datahub");
app.Run();