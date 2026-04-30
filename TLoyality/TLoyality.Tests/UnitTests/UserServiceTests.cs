using Application.Services.Implementations;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Repositories.Abstractions;
using NSubstitute;
using Xunit;

namespace TLoyality.Tests.UnitTests;

public class UserServiceTests
{
    private readonly IUserRepository _userRepositoryMock = Substitute.For<IUserRepository>();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(_userRepositoryMock);
    }

    /// <summary>
    /// Позитивный кейс: Получение всех пользователей с корректным маппингом в UserResponse.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_ShouldReturnMappedResponses_WhenUsersExist()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = 1, FullName = "Иван Иванов", FinancialSegment = FinancialSegment.HIGH },
            new() { Id = 2, FullName = "Петр Петров", FinancialSegment = FinancialSegment.MEDIUM }
        };

        _userRepositoryMock.GetAllAsync().Returns(users);

        // Act
        var result = await _service.GetAllUsers();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainSingle(u => u.FullName == "Иван Иванов");
        result.Should().ContainSingle(u => u.FullName == "Петр Петров");
        await _userRepositoryMock.Received(1).GetAllAsync();
    }

    /// <summary>
    /// Позитивный кейс: Получение внутреннего объекта пользователя по ID.
    /// </summary>
    [Fact]
    public async Task GetUserInternal_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userId = 10;
        var user = new User { Id = userId, FullName = "John Doe" };
        _userRepositoryMock.GetByIdAsync(userId).Returns(user);

        // Act
        var result = await _service.GetUserInternal(userId);

        // Assert
        result.Should().NotBeNull();
        result!.FullName.Should().Be("John Doe");
        result.Id.Should().Be(userId);
    }

    /// <summary>
    /// Негативный кейс: Поиск пользователя по номеру телефона, которого нет в базе.
    /// </summary>
    [Fact]
    public async Task GetUserIdByPhoneNumber_ShouldReturnNull_WhenPhoneNotFound()
    {
        // Arrange
        var phone = "+79990000000";
        _userRepositoryMock.GetUserIdByPhoneNumber(phone).Returns((int?)null);

        // Act
        var result = await _service.GetUserIdByPhoneNumber(phone);

        // Assert
        result.Should().BeNull();
        await _userRepositoryMock.Received(1).GetUserIdByPhoneNumber(phone);
    }

    /// <summary>
    /// Пограничный случай: Запрос пользователя по ID, который не существует (возврат null вместо исключения).
    /// </summary>
    [Fact]
    public async Task GetUserById_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = 999;
        _userRepositoryMock.GetByIdAsync(userId).Returns((User?)null);

        // Act
        var result = await _service.GetUserById(userId);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Проверка маппинга: Поля сущности User должны правильно переноситься в UserResponse.
    /// </summary>
    [Fact]
    public async Task GetUserById_ShouldCorrectlyMapFields_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var user = new User 
        { 
            Id = userId, 
            FullName = "Алексей", 
            Email = "alex@test.com",
            FinancialSegment = FinancialSegment.LOW 
        };
        _userRepositoryMock.GetByIdAsync(userId).Returns(user);

        // Act
        var result = await _service.GetUserById(userId);

        // Assert
        result.Should().NotBeNull();
        result!.FullName.Should().Be(user.FullName);
        // Предполагается, что ToResponse() маппит соответствующие поля
        await _userRepositoryMock.Received(1).GetByIdAsync(userId);
    }
}