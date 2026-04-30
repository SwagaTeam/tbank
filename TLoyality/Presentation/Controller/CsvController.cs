using Application.Services.Abstractions;
using Domain;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller;

/// <summary>
/// Контроллер для массового импорта данных через CSV файлы.
/// Позволяет быстро наполнить базу данных для демонстрации работы системы.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CsvController(ICsvImportService csvService) : ControllerBase
{
    /// <summary>
    /// Загрузить список пользователей из CSV.
    /// </summary>
    /// <param name="file">Файл .csv с данными пользователей.</param>
    /// <response code="200">Возвращает количество успешно импортированных записей.</response>
    [HttpPost("upload-users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadUsers(IFormFile file)
        => Ok(new { Count = await csvService.ImportAsync<User>(file) });

    /// <summary>
    /// Загрузить счета пользователей из CSV.
    /// </summary>
    /// <param name="file">Файл .csv со счетами.</param>
    [HttpPost("upload-accounts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadAccounts(IFormFile file)
        => Ok(new { Count = await csvService.ImportAsync<Accounts>(file) });

    /// <summary>
    /// Загрузить историю выплат кешбэка из CSV. 
    /// Внимание: при загрузке истории автоматически генерируются связанные транзакции для аналитики.
    /// </summary>
    /// <param name="file">Файл .csv с историей выплат.</param>
    [HttpPost("upload-history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadHistory(IFormFile file)
        => Ok(new { Count = await csvService.ImportAsync<LoyaltyHistory>(file) });

    /// <summary>
    /// Загрузить предложения партнеров из CSV.
    /// </summary>
    /// <param name="file">Файл .csv с офферами.</param>
    [HttpPost("upload-offers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadOffers(IFormFile file)
        => Ok(new { Count = await csvService.ImportAsync<Offers>(file) });

    /// <summary>
    /// Загрузить справочник программ лояльности из CSV.
    /// </summary>
    /// <param name="file">Файл .csv с программами.</param>
    [HttpPost("upload-programs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadPrograms(IFormFile file)
        => Ok(new { Count = await csvService.ImportAsync<LoyaltyPrograms>(file) });

    /// <summary>
    /// Комплексная загрузка всей системы. 
    /// Позволяет отправить несколько файлов одновременно для инициализации базы данных.
    /// </summary>
    /// <param name="users">Файл пользователей (необязательно).</param>
    /// <param name="accounts">Файл счетов (необязательно).</param>
    /// <param name="history">Файл истории (необязательно).</param>
    /// <param name="programs">Файл программ лояльности (необязательно).</param>
    /// <param name="offers">Файл предложений партнеров (необязательно).</param>
    /// <response code="200">Общее количество обработанных строк во всех файлах.</response>
    [HttpPost("upload-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        if (programs != null)
        {
            total += await csvService.ImportAsync<LoyaltyPrograms>(programs);
        }

        if (accounts != null)
        {
            total += await csvService.ImportAsync<Accounts>(accounts);
        }

        if (history != null)
        {
            total += await csvService.ImportAsync<LoyaltyHistory>(history);
        }

        if (offers != null)
        {
            total += await csvService.ImportAsync<Offers>(offers);
        }

        return Ok(new { TotalRecords = total });
    }
}