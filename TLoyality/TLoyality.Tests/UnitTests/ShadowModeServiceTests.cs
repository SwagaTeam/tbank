using System.Net;
using System.Text;
using System.Text.Json;
using Application.Models;
using Application.Services.Abstractions;
using Application.Services.Implementations;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace TLoyality.Tests.Tests;

public class ShadowModeServiceTests
{
    private readonly ILoyaltyService _loyaltyService = Substitute.For<ILoyaltyService>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly MockHttpMessageHandler _httpHandler = new();
    private readonly ShadowModeService _service;

    public ShadowModeServiceTests()
    {
        // Создаем HttpClient с моком обработчика
        var httpClient = new HttpClient(_httpHandler);
        _service = new ShadowModeService(httpClient, _loyaltyService, _configuration);
        
        // Устанавливаем переменную окружения для тестов, так как сервис берет её из Environment
        Environment.SetEnvironmentVariable("apiKey", "test-key");
    }

    [Fact]
    public async Task GetShadowRecommendation_ShouldReturnDefault_WhenNoAccountsFound()
    {
        // Arrange
        var userId = 1;
        var context = CreateEmptyContext();
        _loyaltyService.GetShadowContext(userId).Returns(context);

        // Act
        var result = await _service.GetShadowRecommendation(userId);

        // Assert
        result.Should().NotBeNull();
        result!.CurrentStatus.Should().Contain("Привет, Тест Тестович");
        result.LostProfitHighlight.Should().Be("Теневой режим готов к расчету");
    }

    [Fact]
    public async Task GetShadowRecommendation_ShouldReturnResponse_OnSuccessfulApiCall()
    {
        // Arrange
        var userId = 1;
        _loyaltyService.GetShadowContext(userId).Returns(CreatePopulatedContext());

        var jsonResponse = @"
        {
          ""choices"": [{
            ""message"": {
              ""content"": ""{\""current_status\"": \""Вы молодец\"", \""lost_profit_highlight\"": \""Упущено 500р\"", \""recommendation_text\"": \""Перейдите на Black\"", \""target_program_id\"": 2}""
            }
          }]
        }";

        _httpHandler.ResponseContent = jsonResponse;

        // Act
        var result = await _service.GetShadowRecommendation(userId);

        // Assert
        result.Should().NotBeNull();
        result!.TargetProgramId.Should().Be(2);
        result.LostProfitHighlight.Should().Be("Упущено 500р");
    }

    [Fact]
    public async Task GetChatResponse_ShouldSanitizeOutput_WhenApiReturnsSpecialCharacters()
    {
        // Arrange
        var userId = 1;
        _loyaltyService.GetShadowContext(userId).Returns(CreatePopulatedContext());
    
        // Текст, который "прислала" нейронка (с одним слэшем и переносом)
        var aiText = @"Советую /Platinum\ - это выгодно" + "\n" + "Попробуйте!";

        // Формируем структуру ответа OpenRouter
        var apiResponse = new
        {
            choices = new[]
            {
                new { message = new { content = aiText } }
            }
        };

        // Сериализуем объект в JSON — библиотека сама расставит все нужные слэши
        _httpHandler.ResponseContent = JsonSerializer.Serialize(apiResponse);

        // Act
        var result = await _service.GetChatResponse(userId, "Привет");

        // Assert
        result.Should().Be("Советую Platinum - это выгодноПопробуйте!");
    }

    [Fact]
    public async Task GetQuickSavingsHighlightAsync_ShouldReturnCustomMessage_WhenNoTransactions()
    {
        // Arrange
        var userId = 1;
        var context = CreatePopulatedContext();
        context.Transactions.Clear(); // Очищаем транзакции
        _loyaltyService.GetShadowContext(userId).Returns(context);

        // Act
        var result = await _service.GetQuickSavingsHighlightAsync(userId);

        // Assert
        result.Should().Be("В этом месяце вы можете начать экономить, совершив первую покупку.");
    }

    [Fact]
    public async Task GetShadowRecommendation_ShouldThrowException_WhenApiReturnsError()
    {
        // Arrange
        var userId = 1;
        _loyaltyService.GetShadowContext(userId).Returns(CreatePopulatedContext());
        _httpHandler.StatusCode = HttpStatusCode.BadRequest;
        _httpHandler.ResponseContent = "Invalid Request";

        // Act
        var act = () => _service.GetShadowRecommendation(userId);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*OpenRouter API Error*");
    }

    // Вспомогательные методы для генерации тестовых данных
    private ShadowPromptContext CreateEmptyContext()
    {
        return new ShadowPromptContext(
            new User { FullName = "Тест Тестович" },
            new List<Accounts>(),
            new List<LoyaltyHistory>(),
            new List<LoyaltyPrograms>(),
            new List<TransactionResponse>(),
            new List<Offers>()
        );
    }

    private ShadowPromptContext CreatePopulatedContext()
    {
        return new ShadowPromptContext(
            new User { FullName = "Иван Иванов", FinancialSegment = FinancialSegment.MEDIUM },
            new List<Accounts> { new() { AccountId = 1, LoyaltyProgramId = 1, CurrentBalance = 100 } },
            new List<LoyaltyHistory> { new() { AccountId = 1, CashbackAmount = 50 } },
            new List<LoyaltyPrograms> { new() { LoyaltyProgramId = 1, LoyaltyProgramName = LoyaltyProgramName.Black } },
            new List<TransactionResponse> { 
                new() { Amount = 1000, Category = MerchantCategory.Supermarkets, TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow) } 
            },
            new List<Offers> { new() { PartnerName = "ВкусВилл" } }
        );
    }
}

// Вспомогательный класс для мока HttpClient
public class MockHttpMessageHandler : HttpMessageHandler
{
    public string ResponseContent { get; set; } = "{}";
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage
        {
            StatusCode = StatusCode,
            Content = new StringContent(ResponseContent, Encoding.UTF8, "application/json")
        });
    }
}