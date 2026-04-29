using Application.Services.Abstractions;
using Application.Services.Implementations;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller;

[ApiController]
[Route("api/[controller]")]
public class TransactionController(ITransactionService transactionService) : ControllerBase
{
    [HttpGet("streak/{userId}")]
    public async Task<IActionResult> GetTransactionStreak(int userId)
    {
        var streak = await transactionService.GetConsecutiveTransactionsCount(userId);
        return Ok(streak);
    }
}