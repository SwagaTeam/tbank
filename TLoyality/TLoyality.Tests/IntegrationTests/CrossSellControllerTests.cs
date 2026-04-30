using System.Net;
using Application.Services.Abstractions;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Xunit;

namespace TLoyality.Tests.IntegrationTests;

public class CrossSellControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ICrossSellService _crossSellServiceMock = Substitute.For<ICrossSellService>();

    public CrossSellControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Заменяем реальный сервис кросс-продаж моком
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICrossSellService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton(_crossSellServiceMock);
            });
        });
    }

    [Fact]
    public async Task GetPersonalizedOffers_ShouldReturnOk_WhenOffersExist()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = 10;
        var expectedOffers = new List<CrossSellProduct>
        {
            new() { Name = "Т-Инвестиции", Description = "Бонус за открытие счета" },
            new() { Name = "Т-Мобайл", Description = "Бесплатные минуты" }
        };

        _crossSellServiceMock.GetPersonalizedOffersAsync(userId).Returns(expectedOffers);

        // Act
        var response = await client.GetAsync($"/api/CrossSell/offers/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<CrossSellProduct>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result![0].Name.Should().Be("Т-Инвестиции");

        await _crossSellServiceMock.Received(1).GetPersonalizedOffersAsync(userId);
    }

    [Fact]
    public async Task GetPersonalizedOffers_ShouldReturnNotFound_WhenServiceReturnsNull()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = 999;

        // Настраиваем мок на возврат null (согласно логике контроллера это 404)
        _crossSellServiceMock.GetPersonalizedOffersAsync(userId).Returns((IEnumerable<CrossSellProduct>?)null);

        // Act
        var response = await client.GetAsync($"/api/CrossSell/offers/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Message.Should().Be("User or offers not found");
    }

    [Fact]
    public async Task GetPersonalizedOffers_ShouldThrowException_WhenServiceFails()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = 1;

        // Имитируем падение сервиса
        _crossSellServiceMock.GetPersonalizedOffersAsync(userId)
            .Returns(Task.FromException<IEnumerable<CrossSellProduct>>(new Exception("Internal Service Failure")));

        // Act
        // Так как в контроллере нет try-catch, а менять код нельзя, 
        // исключение пробросится напрямую в HttpClient теста
        Func<Task> act = async () => await client.GetAsync($"/api/CrossSell/offers/{userId}");

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Internal Service Failure");
    }

    // Вспомогательный record для типизации ошибки
    private record ErrorResponse(string Message);
}