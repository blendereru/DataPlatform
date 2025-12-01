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
    public EventMessageConsumer(ApplicationContext db, IHubContext<DataHub> hub)
    {
        _db = db;
        _hub = hub;
    }
    
    public async Task Consume(ConsumeContext<EventMessage> context)
    {
        var msg = context.Message;

        var stat = await _db.Stats
            .FirstOrDefaultAsync(s => s.Key == "TotalEvents");

        if (stat == null)
        {
            stat = new Stat { Key = "TotalEvents", Value = "1" };
            _db.Stats.Add(stat);
        }
        else
        {
            stat.Value = (int.Parse(stat.Value) + 1).ToString();
        }

        await _db.SaveChangesAsync();
        
        await _hub.Clients.All.SendAsync("statsUpdated", new 
        {
            totalEvents = stat.Value,
            lastEventPayload = msg.Payload,
            lastEventId = msg.EventId
        });
    }
}