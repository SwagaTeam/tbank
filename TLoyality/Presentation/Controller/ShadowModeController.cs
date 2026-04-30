using Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller;

[ApiController]
[Route("api/shadow-mode")]
public class ShadowModeController(IShadowModeService shadowService) : ControllerBase
{
    [HttpGet("recommendation/{userId}")]
    public async Task<IActionResult> GetRecommendation(int userId)
    {
        var result = await shadowService.GetShadowRecommendation(userId);
        return Ok(result);
    }

    [HttpPost("chat/{userId}")]
    public async Task<IActionResult> AskShadow([FromRoute] int userId, [FromBody] string request)
    {
        if (string.IsNullOrWhiteSpace(request)) 
            return BadRequest("Сообщение не может быть пустым.");

        var response = await shadowService.GetChatResponse(userId, request);
        return Ok(new { text = response });
    }
}
