using Application.Models;
using Application.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller;

/// <summary>
/// Контроллер для управления данными пользователей и их идентификации.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Получить список всех зарегистрированных пользователей системы.
    /// </summary>
    /// <response code="200">Список пользователей успешно получен.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await userService.GetAllUsers();
        return Ok(result);
    }

    /// <summary>
    /// Получить профиль пользователя по его уникальному ID.
    /// </summary>
    /// <param name="userId">Внутренний идентификатор пользователя.</param>
    /// <response code="200">Профиль пользователя найден.</response>
    /// <response code="404">Пользователь с таким ID не существует.</response>
    [HttpGet("id/{userId}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(int userId)
    {
        var result = await userService.GetUserById(userId);

        if (result == null)
            return NotFound(new { Message = $"Пользователь с ID {userId} не найден." });

        return Ok(result);
    }

    /// <summary>
    /// Найти идентификатор пользователя по номеру телефона.
    /// </summary>
    /// <remarks>
    /// Полезно для реализации входа в приложение или поиска контактов. 
    /// Номер должен передаваться без спецсимволов.
    /// </remarks>
    /// <param name="phoneNumber">Номер телефона (например, 79991234567).</param>
    /// <response code="200">ID пользователя найден.</response>
    /// <response code="404">Пользователь с таким номером не зарегистрирован.</response>
    [HttpGet("phone-number/{phoneNumber}")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserByPhoneNumber(string phoneNumber)
    {
        var result = await userService.GetUserIdByPhoneNumber(phoneNumber);

        if (result == null)
            return NotFound(new { Message = "Пользователь с указанным номером не найден." });

        return Ok(result);
    }
}