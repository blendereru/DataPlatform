using DataPlatform.Api.Data;
using DataPlatform.Api.Hubs;
using DataPlatform.Api.Models;
using DataPlatform.Api.Models.Messages;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DataPlatform.Api.Consumers;

public class EventMessageConsumer : IConsumer<EventMessage>
{
    private readonly ApplicationContext _db;
    private readonly IHubContext<DataHub> _hub;
    private readonly ILogger<EventMessageConsumer> _logger;

    public EventMessageConsumer(
        ApplicationContext db,
        IHubContext<DataHub> hub,
        ILogger<EventMessageConsumer> logger)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventMessage> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Consuming EventMessage {@Event}",
            new
            {
                msg.EventId,
                PayloadLength = msg.Payload?.Length ?? 0,
                msg.CreatedAt,
                context.CorrelationId
            }
        );

        try
        {
            var stat = await _db.Stats.FirstOrDefaultAsync(s => s.Key == "TotalEvents");

            if (stat == null)
            {
                stat = new Stat { Key = "TotalEvents", Value = "1" };
                _db.Stats.Add(stat);

                _logger.LogInformation("Stat TotalEvents initialized to 1");
            }
            else
            {
                var prev = stat.Value;
                stat.Value = (int.Parse(stat.Value) + 1).ToString();

                _logger.LogInformation(
                    "TotalEvents incremented from {Prev} to {Next}",
                    prev,
                    stat.Value
                );
            }

            await _db.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("statsUpdated", new
            {
                totalEvents = stat.Value,
                lastEventPayload = msg.Payload,
                lastEventId = msg.EventId
            });

            _logger.LogInformation(
                "Broadcasted statistics update. TotalEvents={Total}, EventId={Id}",
                stat.Value,
                msg.EventId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception while consuming EventMessage {EventId}. CorrelationId={CorrelationId}",
                msg.EventId,
                context.CorrelationId
            );

            throw;
        }
    }
}
