using Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller;

[ApiController]
[Route("api/[controller]")]
public class LoyaltyController(ILoyaltyService loyaltyService) : ControllerBase
{
    [HttpGet("{userId}/summary")]
    public async Task<IActionResult> GetSummary(int userId)
    {
        var result = await loyaltyService.GetUserLoyaltySummaryAsync(userId);
        return Ok(result);
    }
}