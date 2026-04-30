using Application.Models;
using Domain.Entities;

namespace Application.Services.Abstractions;

public interface IUserService
{
    Task<UserResponse?> GetUserById(int userId);
    Task<int?> GetUserIdByPhoneNumber(string phoneNumber);
    Task<ICollection<UserResponse>> GetAllUsers();
    public Task<User?> GetUserInternal(int userId);
}