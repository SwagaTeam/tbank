using System.Net;
using Application.Models;
using Application.Services.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Xunit;

namespace TLoyality.Tests.IntegrationTests;

public class DashboardControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IDashboardService _dashboardServiceMock = Substitute.For<IDashboardService>();

    public DashboardControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Заменяем реальный сервис на мок
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDashboardService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton(_dashboardServiceMock);
            });
        });
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnOk_WithCorrectData()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = 1;
        
        // Создаем тестовый объект DTO
        var expectedDto = new DashboardDto
        {
            UserName = "Иван Иванов",
            AiMessage = "Ваш кешбэк вырос на 10% в этом месяце!",
            LoyaltyAnalytics = new LoyaltyAnalyticsDto { /* заполните поля по необходимости */ },
            Partners = new List<PartnerResponse> 
            { 
                new() { Name = "T-Bank Store" } 
            }
        };

        _dashboardServiceMock.GetDashboardAsync(userId).Returns(expectedDto);

        // Act
        var response = await client.GetAsync($"/api/dashboard/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<DashboardDto>();
        
        result.Should().NotBeNull();
        result!.UserName.Should().Be("Иван Иванов");
        result.AiMessage.Should().Contain("10%");
        result.Partners.Should().HaveCount(1);

        // Проверка: вызывался ли сервис именно с этим ID
        await _dashboardServiceMock.Received(1).GetDashboardAsync(userId);
    }

    [Fact]
    public async Task GetDashboard_WithInvalidIdType_ShouldReturnNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidId = "abc"; // Строка вместо int

        // Act
        var response = await client.GetAsync($"/api/dashboard/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}