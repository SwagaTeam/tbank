using Application.Services.Abstractions;
using Domain;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller;

[ApiController]
[Route("api/[controller]")]
public class CsvController(ICsvImportService csvService) : ControllerBase
{
    [HttpPost("upload-users")]
    public async Task<IActionResult> UploadUsers(IFormFile file) 
        => Ok(new { Count = await csvService.ImportAsync<User>(file) });

    [HttpPost("upload-accounts")]
    public async Task<IActionResult> UploadAccounts(IFormFile file) 
        => Ok(new { Count = await csvService.ImportAsync<Accounts>(file) });

    [HttpPost("upload-history")]
    public async Task<IActionResult> UploadHistory(IFormFile file) 
        => Ok(new { Count = await csvService.ImportAsync<LoyaltyHistory>(file) });
    
    [HttpPost("upload-offers")]
    public async Task<IActionResult> UploadOffers(IFormFile file) 
        => Ok(new { Count = await csvService.ImportAsync<Offers>(file) });
    
    [HttpPost("upload-programs")]
    public async Task<IActionResult> UploadPrograms(IFormFile file) 
        => Ok(new { Count = await csvService.ImportAsync<LoyaltyPrograms>(file) });

    [HttpPost("upload-all")]
    public async Task<IActionResult> UploadAll(
        IFormFile? users, 
        IFormFile? accounts, 
        IFormFile? history, 
        IFormFile? programs, 
        IFormFile? offers)
    {
        var total = 0;
        if (users != null)
        {
            total += await csvService.ImportAsync<User>(users);
        }
        
        if (accounts != null)
        {
            total += await csvService.ImportAsync<Accounts>(accounts);
        }
        
        if (history != null)
        {
            total += await csvService.ImportAsync<LoyaltyHistory>(history);
        }

        if (programs != null)
        {
            total += await csvService.ImportAsync<LoyaltyPrograms>(programs);
        }

        if (offers != null)
        {
            total += await csvService.ImportAsync<Offers>(offers);
        }

        return Ok(new { TotalRecords = total });
    }
}