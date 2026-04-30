using System.Net;
using Application.Models;
using Application.Services.Abstractions;
using FluentAssertions;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace TLoyality.Tests.IntegrationTests;

public class UserControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IUserService _userServiceMock = Substitute.For<IUserService>();

    public UserControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var dbContextDescriptor =
                    services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

                services.AddDbContext<AppDbContext>(options => { options.UseInMemoryDatabase("TestDb"); });

                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton(_userServiceMock);
            });
        });
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnOk_WithUsersList()
    {
        // Arrange
        var client = _factory.CreateClient();
        var expectedUsers = new List<UserResponse>
        {
            new() { FullName = "Алексей Петров", Email = "alex@test.ru", PhoneNumber = "79001112233" }
        };

        // Настройка в стиле NSubstitute: Объект.Метод(Аргументы).Returns(Результат)
        _userServiceMock.GetAllUsers().Returns(expectedUsers);

        // Act
        var response = await client.GetAsync("/api/user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<UserResponse>>();
        result.Should().BeEquivalentTo(expectedUsers);
    }

    [Fact]
    public async Task GetUserById_WhenUserExists_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = 42;
        var userResponse = new UserResponse { FullName = "Джон Доу", Email = "john@doe.com" };

        _userServiceMock.GetUserById(userId).Returns(userResponse);

        // Act
        var response = await client.GetAsync($"/api/user/id/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserResponse>();
        result.Should().NotBeNull();
        result!.FullName.Should().Be("Джон Доу");
    }

    [Fact]
    public async Task GetUserById_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = 999;
        // В NSubstitute по умолчанию возвращается null для ссылочных типов, 
        // но для ясности можно указать явно:
        _userServiceMock.GetUserById(userId).Returns((UserResponse?)null);

        // Act
        var response = await client.GetAsync($"/api/user/id/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserByPhoneNumber_WhenValid_ShouldReturnUserId()
    {
        // Arrange
        var client = _factory.CreateClient();
        var phone = "79991234567";
        var expectedId = 777;

        _userServiceMock.GetUserIdByPhoneNumber(phone).Returns(expectedId);

        // Act
        var response = await client.GetAsync($"/api/user/phone-number/{phone}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<int>();
        result.Should().Be(expectedId);

        // Дополнительная проверка: был ли вызван метод именно с этим номером
        await _userServiceMock.Received(1).GetUserIdByPhoneNumber(phone);
    }
}