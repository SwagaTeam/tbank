using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller
{
    /// <summary>
    /// Управление кросс-селл предложениями экосистемы
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CrossSellController(ICrossSellService crossSellService) : ControllerBase
    {
        /// <summary>
        /// Получить персонализированные предложения (Т-Инвестиции, Мобайл и т.д.) на основе сегмента пользователя
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Список подходящих продуктов</returns>
        [HttpGet("offers/{userId}")]
        [ProducesResponseType(typeof(IEnumerable<CrossSellProduct>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPersonalizedOffers(int userId)
        {
            var offers = await crossSellService.GetPersonalizedOffersAsync(userId);

            if (offers == null)
                return NotFound(new { Message = "User or offers not found" });

            return Ok(offers);
        }
    }
}