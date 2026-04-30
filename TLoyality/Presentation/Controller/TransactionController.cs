using Application.Services.Abstractions;
using Application.Services.Implementations;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller;

[ApiController]
[Route("api/[controller]")]
public class TransactionController(ITransactionService transactionService) : ControllerBase
{
    [HttpGet("streak/{accountId}")]
    public async Task<IActionResult> GetTransactionStreak(int accountId)
    {
        var streak = await transactionService.GetConsecutiveTransactionsCount(accountId);
        return Ok(streak);
    }
}