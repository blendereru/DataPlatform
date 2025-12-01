using DataPlatform.Api.Data;
using DataPlatform.Api.Models;
using DataPlatform.Api.Models.Messages;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace DataPlatform.Api.Controllers;

public class EventsController : Controller
{
    private readonly ApplicationContext _db;
    private readonly IPublishEndpoint _bus;
    public EventsController(ApplicationContext db, IPublishEndpoint bus)
    {
        _db = db;
        _bus = bus;
    }

    [HttpGet("/events")]
    public IActionResult Index()
    {
        var events = _db.Events.OrderByDescending(e => e.Id).ToList();
        return View(events); 
    }

    [HttpPost("/api/events")]
    public async Task<IActionResult> Create([FromBody] string payload)
    {
        var entity = new EventEntity { Payload = payload };
        _db.Events.Add(entity);
        await _db.SaveChangesAsync();

        await _bus.Publish(new EventMessage 
        {
            EventId = entity.Id,
            Payload = payload,
            CreatedAt = entity.CreatedAt
        });

        return Ok(entity);
    }
}