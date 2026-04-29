using Application.Models;
using Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controller;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await userService.GetAllUsers();
        return Ok(result);
    }

    [HttpGet("id/{userId}")]
    public async Task<IActionResult> GetUserById(int userId)
    {
        var result = await userService.GetUserById(userId);
        return Ok(result);
    }

    [HttpGet("phone-number/{phoneNumber}")]
    public async Task<IActionResult> GetUserByPhoneNumber(string phoneNumber)
    {
        var result = await userService.GetUserIdByPhoneNumber(phoneNumber);
        return Ok(result);
    }
}