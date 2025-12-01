using Microsoft.AspNetCore.Mvc;

namespace DataPlatform.Api.Controllers;

public class DashboardController : Controller
{
    [HttpGet("/dashboard")]
    public IActionResult Index()
    {
        return View();
    }
}