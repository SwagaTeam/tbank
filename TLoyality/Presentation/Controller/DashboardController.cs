using Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller;

[ApiController]
[Route("api/[controller]")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetDashboard(int userId)
    {
        var result = await dashboardService.GetDashboardAsync(userId);
        return Ok(result);
    }
}