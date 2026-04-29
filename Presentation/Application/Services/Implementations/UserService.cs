using Application.Models;
using Application.Services.Abstractions;
using Infrastructure.Repositories.Abstractions;

namespace Application.Services.Implementations;

public class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<ICollection<UserResponse>> GetAllUsers()
    {
        var users = await userRepository.GetAllAsync();
        return users.Select(x => x.ToResponse()).ToList();
    }

    public async Task<int?> GetUserIdByPhoneNumber(string phoneNumber)
    {
       return await userRepository.GetUserIdByPhoneNumber(phoneNumber);
    }

    public async Task<UserResponse?> GetUserById(int userId)
    {
        var result = await userRepository.GetByIdAsync(userId);
        return result?.ToResponse();
    }
}