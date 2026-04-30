using System.Net;
using Application.Models;
using Application.Services.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Xunit;

namespace TLoyality.Tests.IntegrationTests;

public class LoyaltyControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ILoyaltyService _loyaltyServiceMock = Substitute.For<ILoyaltyService>();

    public LoyaltyControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Подменяем реальный сервис аналитики лояльности моком
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ILoyaltyService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton(_loyaltyServiceMock);
            });
        });
    }

    [Fact]
    public async Task GetSummary_ShouldReturnOk_WithFullAnalyticsData()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = 1;

        var expectedAnalytics = new LoyaltyAnalyticsDto
        {
            TotalRub = 1250.50m,
            TotalMiles = 5400,
            TotalBravo = 320,
            CurrentMonthEarned = 450.00m,
            RecommendedCategoryName = "Супермаркеты",
            PotentialCategorySavings = 850.00m,
            Last9MonthsLabels = new List<string> { "Янв", "Фев", "Мар" },
            Last9MonthsValues = new List<decimal> { 100.5m, 200.0m, 150.0m },
            MonthlyHistory = new List<HistoryPointDto>
            {
                new(new DateOnly(2024, 3, 1), 150.0m, "RUB")
            }
        };

        _loyaltyServiceMock.GetUserLoyaltySummaryAsync(userId).Returns(expectedAnalytics);

        // Act
        var response = await client.GetAsync($"/api/loyalty/{userId}/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LoyaltyAnalyticsDto>();

        result.Should().NotBeNull();
        result!.TotalRub.Should().Be(1250.50m);
        result.RecommendedCategoryName.Should().Be("Супермаркеты");
        result.Last9MonthsLabels.Should().HaveCount(3);
        result.MonthlyHistory.Should().ContainSingle(x => x.Currency == "RUB");

        // Проверка: был ли вызван именно этот метод сервиса
        await _loyaltyServiceMock.Received(1).GetUserLoyaltySummaryAsync(userId);
    }

    [Fact]
    public async Task GetSummary_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = 999;

        // Настраиваем мок на возврат null
        _loyaltyServiceMock.GetUserLoyaltySummaryAsync(userId).Returns((LoyaltyAnalyticsDto?)null);

        // Act
        var response = await client.GetAsync($"/api/loyalty/{userId}/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Читаем через типизированный record вместо Dictionary
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        error.Should().NotBeNull();
        error!.Message.Should().Contain($"ID {userId} не найдена");
    }

    [Fact]
    public async Task GetSummary_WithInvalidUserId_ShouldReturnBadRequest() // Переименовали
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidId = "user_123";

        // Act
        var response = await client.GetAsync($"/api/loyalty/{invalidId}/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private record ErrorResponse(string Message);
}