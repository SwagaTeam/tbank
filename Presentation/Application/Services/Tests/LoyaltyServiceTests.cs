using Application.Models;
using Application.Services.Abstractions;
using Application.Services.Implementations;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Repositories.Abstractions;
using NSubstitute;
using Xunit;

namespace Application.Services.Tests;

public class LoyaltyServiceTests
{
    private readonly IAccountRepository _accountRepo = Substitute.For<IAccountRepository>();
    private readonly ILoyaltyHistoryRepository _historyRepo = Substitute.For<ILoyaltyHistoryRepository>();
    private readonly IRepository<LoyaltyPrograms> _programRepo = Substitute.For<IRepository<LoyaltyPrograms>>();
    private readonly IOfferRepository _offerRepo = Substitute.For<IOfferRepository>();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly ITransactionService _transactionService = Substitute.For<ITransactionService>();
    
    private readonly LoyaltyService _service;

    public LoyaltyServiceTests()
    {
        _service = new LoyaltyService(
            _accountRepo, _historyRepo, _programRepo, 
            _offerRepo, _userService, _transactionService);
    }

    [Fact]
    public async Task GetUserLoyaltySummaryAsync_ShouldCalculateCorrectBalances_ForMultiplePrograms()
    {
        // Arrange
        var userId = 1;
        var account1 = new Accounts { AccountId = 101, LoyaltyProgramId = 1, ReferalBalance = 100 }; // Black
        var account2 = new Accounts { AccountId = 102, LoyaltyProgramId = 2, ReferalBalance = 50 };  // Miles
        
        _accountRepo.GetByUserIdAsync(userId).Returns(new List<Accounts> { account1, account2 });
        _programRepo.GetAllAsync().Returns(new List<LoyaltyPrograms>
        {
            new() { LoyaltyProgramId = 1, LoyaltyProgramName = LoyaltyProgramName.Black },
            new() { LoyaltyProgramId = 2, LoyaltyProgramName = LoyaltyProgramName.AllAirlines }
        });
        
        _historyRepo.GetByAccountIdsAsync(Arg.Any<List<int>>()).Returns(new List<LoyaltyHistory>
        {
            new() { AccountId = 101, CashbackAmount = 500 },
            new() { AccountId = 102, CashbackAmount = 2000 }
        });

        // Act
        var result = await _service.GetUserLoyaltySummaryAsync(userId);

        // Assert
        result.TotalRub.Should().Be(500);
        result.TotalMiles.Should().Be(2000);
        result.TotalReferal.Should().Be(10000);
    }

    [Fact]
    public async Task GetUserLoyaltySummaryAsync_ShouldPredictBenefit_BasedOnLastMonthSpend()
    {
        // Arrange
        var userId = 1;
        var now = DateTime.UtcNow;
        var lastMonthDate = now.AddMonths(-1);
        
        _accountRepo.GetByUserIdAsync(userId).Returns(new List<Accounts> { new() { AccountId = 1 } });
        _transactionService.GetCashbackBonusRate(1).Returns(0.02m); // 0.02% -> 0.0002 в расчетах
        
        // Транзакция на 10 000 в прошлом месяце
        var transactions = new List<TransactionResponse>
        {
            new() { 
                Amount = 10000, 
                TransactionDate = DateOnly.FromDateTime(lastMonthDate),
                Category = MerchantCategory.Supermarkets 
            }
        };
        _transactionService.GetByAccountIdsAsync(Arg.Any<List<int>>()).Returns(transactions);
        _programRepo.GetAllAsync().Returns(new List<LoyaltyPrograms> { new() { LoyaltyProgramId = 0, LoyaltyProgramName = LoyaltyProgramName.Black }});

        // Act
        var result = await _service.GetUserLoyaltySummaryAsync(userId);

        // Assert
        // Rate = 0.01 (стандарт) + 0.0002 (бонус) = 0.0102
        // Benefit = 10000 * 3 * 0.0102 = 306
        result.PredictedBenefit3Months.Should().Be(306);
        result.RecommendedCategoryName.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUserLoyaltySummaryAsync_ShouldReturnEmptyDto_WhenNoAccountsFound()
    {
        // Arrange
        _accountRepo.GetByUserIdAsync(1).Returns(new List<Accounts>());

        // Act
        var result = await _service.GetUserLoyaltySummaryAsync(1);

        // Assert
        result.TotalRub.Should().Be(0);
        result.Last9MonthsLabels.Should().BeEmpty();
    }

    [Fact]
    public async Task GetShadowContext_ShouldThrowUnauthorized_WhenUserNotFound()
    {
        // Arrange
        _userService.GetUserInternal(1).Returns((User)null!);

        // Act
        var act = () => _service.GetShadowContext(1);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetShadowContext_ShouldAssembleFullContext_WhenDataExists()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, FinancialSegment = FinancialSegment.HIGH };
        _userService.GetUserInternal(userId).Returns(user);
        _accountRepo.GetByUserIdAsync(userId).Returns(new List<Accounts> { new() { AccountId = 1 } });
        
        // Act
        var context = await _service.GetShadowContext(userId);

        // Assert
        context.Should().NotBeNull();
        context.User.FullName.Should().Be(user.FullName);
        await _offerRepo.Received(1).GetPartnersAsync(FinancialSegment.HIGH);
    }
}