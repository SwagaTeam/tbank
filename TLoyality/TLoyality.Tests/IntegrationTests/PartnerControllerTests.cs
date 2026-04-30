using System.Net;
using Application.Models;
using Application.Services.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Xunit;

namespace TLoyality.Tests.IntegrationTests;

public class PartnerControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IPartnerService _partnerServiceMock = Substitute.For<IPartnerService>();

    public PartnerControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Подменяем реальный сервис на мок
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPartnerService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton(_partnerServiceMock);
            });
        });
    }

    [Fact]
    public async Task GetPartners_ShouldReturnOk_WhenPartnersExist()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = 1;
        var expectedPartners = new List<PartnerResponse>
        {
            new() { Name = "T-Store", CashbackPercent = 10.5m },
            new() { Name = "Super-Market", CashbackPercent = 5.0m }
        };

        _partnerServiceMock.GetSortedPartnersAsync(userId).Returns(expectedPartners);

        // Act
        var response = await client.GetAsync($"/api/partner/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<PartnerResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result![0].Name.Should().Be("T-Store");

        await _partnerServiceMock.Received(1).GetSortedPartnersAsync(userId);
    }

    [Fact]
    public async Task GetPartners_ShouldReturnNotFound_WhenListIsEmpty()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = 777;

        // Настраиваем пустой список
        _partnerServiceMock.GetSortedPartnersAsync(userId).Returns(new List<PartnerResponse>());

        // Act
        var response = await client.GetAsync($"/api/partner/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Message.Should().Be("Для данного пользователя предложения не найдены.");
    }

    [Fact]
    public async Task GetPartners_WithInvalidIdType_ShouldReturnNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidId = "not-an-int";

        // Act
        var response = await client.GetAsync($"/api/partner/{invalidId}");

        // Assert
        // Т.к. в контроллере стоит {userId:int}, несовпадение типа ведет к 404
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Вспомогательный record для чтения ошибок
    private record ErrorResponse(string Message);
}