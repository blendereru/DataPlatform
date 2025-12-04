using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataPlatform.Api.Controllers;

[Authorize(Policy = "AdminOnly")]
public class DashboardController : Controller
{
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ILogger<DashboardController> logger)
    {
        _logger = logger;
    }

    [HttpGet("/dashboard")]
    public IActionResult Index()
    {
        try
        {
            _logger.LogInformation(
                "Admin dashboard accessed by {@User}",
                new {
                    User.Identity?.Name,
                    Claims = User.Claims.Select(c => new { c.Type, c.Value })
                }
            );

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while loading dashboard for {@User}",
                new {
                    Name = User.Identity?.Name,
                    Claims = User.Claims.Select(c => new { c.Type, c.Value })
                });

            return StatusCode(500, "Unexpected error");
        }
    }
}