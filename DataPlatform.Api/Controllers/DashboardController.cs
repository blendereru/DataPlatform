using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataPlatform.Api.Controllers;

[Authorize(Policy = "AdminOnly")]
public class DashboardController : Controller
{
    [HttpGet("/dashboard")]
    public IActionResult Index()
    {
        return View();
    }
}