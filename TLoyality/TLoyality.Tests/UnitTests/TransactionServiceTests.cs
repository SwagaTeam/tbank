using Application.Models;
using Application.Services.Implementations;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Repositories.Abstractions;
using NSubstitute;
using Xunit;

namespace Application.Services.Tests;

public class TransactionServiceTests
{
    private readonly ITransactionRepository _repositoryMock = Substitute.For<ITransactionRepository>();
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _service = new TransactionService(_repositoryMock);
    }

    /// <summary>
    /// Позитивный кейс: Проверка корректного подсчета дней подряд без пропусков.
    /// </summary>
    [Fact]
    public async Task GetConsecutiveTransactionsCount_ShouldReturnCorrectCount_WhenDaysAreConsecutive()
    {
        // Arrange
        var accountId = 1;
        var startDate = new DateOnly(2023, 10, 1);
        var transactions = new List<Transaction>
        {
            new() { TransactionDate = startDate },
            new() { TransactionDate = startDate.AddDays(1) },
            new() { TransactionDate = startDate.AddDays(2) }
        };

        _repositoryMock.GetByAccountIdsAsync(Arg.Any<ICollection<int>>()).Returns(transactions);

        // Act
        var result = await _service.GetConsecutiveTransactionsCount(accountId);

        // Assert
        result.Should().Be(3);
    }

    /// <summary>
    /// Негативный кейс / Логика сброса: Счетчик должен сброситься при пропуске более чем в 1 день.
    /// </summary>
    [Fact]
    public async Task GetConsecutiveTransactionsCount_ShouldResetStreak_WhenThereIsGap()
    {
        // Arrange
        var startDate = new DateOnly(2023, 10, 1);
        var transactions = new List<Transaction>
        {
            new() { TransactionDate = startDate },
            new() { TransactionDate = startDate.AddDays(1) },
            new() { TransactionDate = startDate.AddDays(3) }, // Пропуск 1 дня (2-е число)
            new() { TransactionDate = startDate.AddDays(4) }
        };

        _repositoryMock.GetByAccountIdsAsync(Arg.Any<ICollection<int>>()).Returns(transactions);

        // Act
        var result = await _service.GetConsecutiveTransactionsCount(1);

        // Assert
        result.Should().Be(2);
    }

    /// <summary>
    /// Пограничный случай: Ровно 7 дней подряд должны давать бонус 0.5%.
    /// </summary>
    [Fact]
    public async Task GetCashbackBonusRate_ShouldReturnHalfPercent_WhenStreakIsExactlySevenDays()
    {
        // Arrange
        var startDate = new DateOnly(2023, 1, 1);
        var transactions = Enumerable.Range(0, 7)
            .Select(i => new Transaction { TransactionDate = startDate.AddDays(i) })
            .ToList();

        _repositoryMock.GetByAccountIdsAsync(Arg.Any<ICollection<int>>()).Returns(transactions);

        // Act
        var result = await _service.GetCashbackBonusRate(1);

        // Assert
        result.Should().Be(0.5m);
    }

    /// <summary>
    /// Пограничный случай: 13 дней подряд все еще должны давать только 0.5% (второй период не завершен).
    /// </summary>
    [Fact]
    public async Task GetCashbackBonusRate_ShouldNotIncreaseBonus_UntilFullPeriodIsReached()
    {
        // Arrange
        var transactions = Enumerable.Range(0, 13)
            .Select(i => new Transaction { TransactionDate = new DateOnly(2023, 1, 1).AddDays(i) })
            .ToList();

        _repositoryMock.GetByAccountIdsAsync(Arg.Any<ICollection<int>>()).Returns(transactions);

        // Act
        var result = await _service.GetCashbackBonusRate(1);

        // Assert
        result.Should().Be(0.5m);
    }

    /// <summary>
    /// Проверка маппинга: Данные из БД должны корректно переходить в Response модель.
    /// </summary>
    [Fact]
    public async Task GetByAccountIdsAsync_ShouldCorrectlyMapToResponse()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new() 
            { 
                AccountId = 1, 
                Amount = 1500.50m, 
                Category = MerchantCategory.Restaurants, 
                TransactionDate = new DateOnly(2023, 10, 5) 
            }
        };

        _repositoryMock.GetByAccountIdsAsync(Arg.Any<ICollection<int>>()).Returns(transactions);

        // Act
        var result = await _service.GetByAccountIdsAsync(new List<int> { 1 });

        // Assert
        result.Should().HaveCount(1);
        result.First().Amount.Should().Be(1500.50m);
        result.First().Category.Should().Be(MerchantCategory.Restaurants);
    }
}