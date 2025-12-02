using DataPlatform.Api.Data;
using DataPlatform.Api.Models;
using DataPlatform.Api.Models.Messages;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataPlatform.Api.Controllers;

///
/// Controller responsible for viewing and creating event records.
/// All endpoints require authentication.
///
[Authorize]
public class EventsController : Controller
{
    private readonly ApplicationContext _db;
    private readonly IPublishEndpoint _bus;

    ///
    /// Creates a new instance of the <see cref="EventsController"/>.
    ///
    /// <param name="db">Database context used for accessing events.</param>
    /// <param name="bus">Message bus interface used to publish event messages.</param>
    public EventsController(ApplicationContext db, IPublishEndpoint bus)
    {
        _db = db;
        _bus = bus;
    }

    ///
    /// Gets a list of existing events ordered by newest first.
    /// This endpoint returns a Razor view and is not intended for API clients.
    ///
    /// <returns>Renders a view containing the list of events.</returns>
    [HttpGet("/events")]
    public IActionResult Index()
    {
        var events = _db.Events.OrderByDescending(e => e.Id).ToList();
        return View(events); 
    }

    ///
    /// Creates a new event and publishes a corresponding message to the event bus.
    ///
    /// <param name="payload">Raw event payload submitted by the caller.</param>
    /// <returns>
    /// Returns the created <see cref="EventEntity"/> including its generated identifier.
    /// </returns>
    /// <response code="200">Event was successfully created.</response>
    /// <response code="400">Payload was invalid.</response>
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
