using Application.Models;
using Application.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller;

/// <summary>
/// Контроллер для управления аналитикой лояльности и кешбэка.
/// Предоставляет данные для дашбордов и прогнозных моделей.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LoyaltyController(ILoyaltyService loyaltyService) : ControllerBase
{
    /// <summary>
    /// Получить сводную аналитику лояльности пользователя.
    /// </summary>
    /// <remarks>
    /// Метод возвращает агрегированные балансы (рубли, мили, бонусы), 
    /// историю начислений за последние 9 месяцев и прогноз потенциальной выгоды 
    /// на основе текущих трат.
    /// </remarks>
    /// <param name="userId">Уникальный идентификатор пользователя в системе.</param>
    /// <response code="200">Успешное получение аналитики.</response>
    /// <response code="404">Пользователь или активные счета не найдены.</response>
    /// <response code="500">Внутренняя ошибка сервера при расчете прогнозов.</response>
    [HttpGet("{userId}/summary")]
    [ProducesResponseType(typeof(LoyaltyAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSummary(int userId)
    {
        var result = await loyaltyService.GetUserLoyaltySummaryAsync(userId);

        // В рамках хакатона: если у пользователя нет счетов, возвращаем 404 для чистоты API
        if (result == null)
        {
            return NotFound(new { Message = $"Аналитика для пользователя с ID {userId} не найдена." });
        }

        return Ok(result);
    }
}