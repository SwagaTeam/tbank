using Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller;

[ApiController]
[Route("api/[controller]")]
public class PartnerController(IPartnerService partnerService) : ControllerBase
{
    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetPartnersAsync(int userId)
    {
        var result = await partnerService.GetSortedPartnersAsync(userId);
        return Ok(result);
    }
}