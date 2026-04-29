using Domain;

namespace Application.Models;

/// <summary>
/// Публичный профиль пользователя.
/// </summary>
public class UserResponse
{
    /// <summary> Полное имя (ФИО). </summary>
    /// <example>Иванов Иван Иванович</example>
    public string FullName { get; set; } = string.Empty;

    /// <summary> Адрес электронной почты. </summary>
    /// <example>user@example.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary> Контактный номер телефона. </summary>
    /// <example>79001234567</example>
    public string PhoneNumber { get; set; } = string.Empty;
}

public static class UserMapper
{
    public static UserResponse ToResponse(this User user)
    {
        return new UserResponse
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
        };
    }
}