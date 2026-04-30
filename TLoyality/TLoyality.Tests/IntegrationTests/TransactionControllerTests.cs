using System.Net;
using Application.Services.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Xunit;

namespace TLoyality.Tests.IntegrationTests;

public class TransactionControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITransactionService _transactionServiceMock = Substitute.For<ITransactionService>();

    public TransactionControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Заменяем реальный сервис транзакций моком
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITransactionService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton(_transactionServiceMock);
            });
        });
    }

    [Fact]
    public async Task GetTransactionStreak_ShouldReturnCount_WhenAccountExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var accountId = 42;
        var expectedStreak = 5; // Например, 5 дней подряд были транзакции

        _transactionServiceMock.GetConsecutiveTransactionsCount(accountId).Returns(expectedStreak);

        // Act
        var response = await client.GetAsync($"/api/transaction/streak/{accountId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Читаем простое число из ответа
        var result = await response.Content.ReadFromJsonAsync<int>();
        result.Should().Be(expectedStreak);

        await _transactionServiceMock.Received(1).GetConsecutiveTransactionsCount(accountId);
    }

    [Fact]
    public async Task GetTransactionStreak_WithInvalidAccountId_ShouldReturnBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidId = "not-a-number";

        // Act
        var response = await client.GetAsync($"/api/transaction/streak/{invalidId}");

        // Assert
        // Т.к. в контроллере параметр int accountId, ModelBinder вернет 400 при попытке маппинга строки
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransactionStreak_ShouldHandleZero_WhenNoTransactions()
    {
        // Arrange
        var client = _factory.CreateClient();
        var accountId = 100;
        _transactionServiceMock.GetConsecutiveTransactionsCount(accountId).Returns(0);

        // Act
        var response = await client.GetAsync($"/api/transaction/streak/{accountId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<int>();
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetTransactionStreak_ShouldThrowException_WhenServiceFails()
    {
        // Arrange
        var client = _factory.CreateClient();
        _transactionServiceMock.GetConsecutiveTransactionsCount(Arg.Any<int>())
            .Returns(Task.FromException<int>(new Exception("Database connection error")));

        // Act
        Func<Task> act = async () => await client.GetAsync("/api/transaction/streak/1");

        // Assert
        // Мы ожидаем, что исключение "вылетит" из контроллера наружу в тест
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Database connection error");
    }
}