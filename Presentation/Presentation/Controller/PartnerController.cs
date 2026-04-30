using Application.Models;
using Application.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller;

/// <summary>
/// Контроллер для работы с партнерскими предложениями и спецпредложениями.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PartnerController(IPartnerService partnerService) : ControllerBase
{
    /// <summary>
    /// Получить список доступных партнеров, отсортированных по релевантности для пользователя.
    /// </summary>
    /// <remarks>
    /// Сортировка учитывает финансовый сегмент пользователя (HIGH/MEDIUM/LOW). 
    /// Первыми возвращаются партнеры с наиболее высоким процентом кешбэка и те, 
    /// которые наиболее соответствуют профилю трат клиента.
    /// </remarks>
    /// <param name="userId">Идентификатор пользователя для персонализации списка.</param>
    /// <response code="200">Список партнеров успешно сформирован.</response>
    /// <response code="404">Пользователь не найден или для его сегмента нет доступных предложений.</response>
    [HttpGet("{userId:int}")]
    [ProducesResponseType(typeof(IEnumerable<PartnerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPartnersAsync(int userId)
    {
        var result = await partnerService.GetSortedPartnersAsync(userId);

        if (result.Count == 0)
        {
            return NotFound(new { Message = "Для данного пользователя предложения не найдены." });
        }

        return Ok(result);
    }
}