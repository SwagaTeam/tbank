using Domain;

namespace Application.Models;

public class UserResponse
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
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