using DataPlatform.Api.Data;
using DataPlatform.Api.DTOs;
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
    private readonly ILogger<EventsController> _logger;

    ///
    /// Creates a new instance of the <see cref="EventsController"/>.
    ///
    /// <param name="db">Database context used for accessing events.</param>
    /// <param name="bus">Message bus interface used to publish event messages.</param>
    public EventsController(ApplicationContext db, IPublishEndpoint bus, ILogger<EventsController> logger)
    {
        _db = db;
        _bus = bus;
        _logger = logger;
    }

    ///
    /// Gets a list of existing events ordered by newest first.
    /// This endpoint returns a Razor view and is not intended for API clients.
    ///
    /// <returns>Renders a view containing the list of events.</returns>
    [HttpGet("/events")]
    public IActionResult Index()
    {
        _logger.LogInformation("Fetching events list for user {User}", User.Identity?.Name);
        
        var events = _db.Events.OrderByDescending(e => e.Id).ToList();
        
        _logger.LogInformation("Loaded {Count} events for dashboard", events.Count);
        
        return View(events); 
    }

    /// <summary>
    /// Creates a new event and publishes a corresponding message to the event bus.
    /// </summary>
    /// <param name="request">Model containing the event payload to store and publish.</param>
    /// <returns>
    /// Returns the created <see cref="EventEntity"/> including its generated identifier.
    /// </returns>
    /// <response code="200">Event was successfully created.</response>
    /// <response code="400">The request payload was invalid.</response>
    [HttpPost("/api/events")]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        _logger.LogInformation(
            "Received CreateEvent request from {User}. PayloadLength={Len}",
            User.Identity?.Name,
            request?.Payload?.Length ?? 0
        );

        if (request == null || string.IsNullOrWhiteSpace(request.Payload))
        {
            _logger.LogWarning(
                "CreateEvent request rejected: empty payload. User={User}",
                User.Identity?.Name
            );

            return BadRequest("Payload cannot be empty.");
        }

        try
        {
            var entity = new EventEntity
            {
                Payload = request.Payload
            };

            _db.Events.Add(entity);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Created new EventEntity with Id={Id} at {CreatedAt}",
                entity.Id,
                entity.CreatedAt
            );

            var message = new EventMessage
            {
                EventId = entity.Id,
                Payload = request.Payload,
                CreatedAt = entity.CreatedAt
            };

            await _bus.Publish(message);

            _logger.LogInformation(
                "Published EventMessage for EventId={Id}",
                entity.Id
            );

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while creating event. User={User}",
                User.Identity?.Name
            );

            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}