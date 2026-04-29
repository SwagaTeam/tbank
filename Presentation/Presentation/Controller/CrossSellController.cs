using Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrossSellController(ICrossSellService crossSellService) : ControllerBase
    {
        [HttpGet("offers/{userId}")]
        public async Task<IActionResult> GetPersonalizedOffers(int userId)
        {
            var offers = await crossSellService.GetPersonalizedOffersAsync(userId);
            return Ok(offers);
        }
    }
}
