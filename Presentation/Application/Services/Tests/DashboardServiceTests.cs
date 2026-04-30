using Application.Models;
using Application.Services.Abstractions;
using Application.Services.Implementations;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Application.Services.Tests;

public class DashboardServiceTests
{
    private readonly ILoyaltyService _loyaltyMock;
    private readonly IUserService _userMock;
    private readonly IPartnerService _partnerMock;
    private readonly IShadowModeService _aiMock;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _loyaltyMock = Substitute.For<ILoyaltyService>();
        _userMock = Substitute.For<IUserService>();
        _partnerMock = Substitute.For<IPartnerService>();
        _aiMock = Substitute.For<IShadowModeService>();

        _service = new DashboardService(
            _loyaltyMock, 
            _userMock, 
            _partnerMock, 
            _aiMock);
    }

    [Fact]
    public async Task GetDashboardAsync_ShouldReturnCorrectDto_WhenDataIsValid()
    {
        // Arrange
        var userId = 77;
        var user = new User { FullName = "Александр Пушкин" };
        var loyalty = new LoyaltyAnalyticsDto { TotalRub = 1000, PredictedBenefit3Months = 5000 };
        var partners = Enumerable.Range(1, 10)
            .Select(i => new PartnerResponse { Name = $"Partner {i}" })
            .ToList();
        var aiHint = "Вы тратите много на книги. Активируйте категорию 'Образование'.";

        _userMock.GetUserInternal(userId).Returns(user);
        _loyaltyMock.GetUserLoyaltySummaryAsync(userId, false).Returns(loyalty);
        _partnerMock.GetSortedPartnersAsync(userId).Returns(partners);
        _aiMock.GetQuickSavingsHighlightAsync(userId).Returns(aiHint);

        // Act
        var result = await _service.GetDashboardAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be("Александр Пушкин");
        result.AiMessage.Should().Be(aiHint);
        result.LoyaltyAnalytics.TotalRub.Should().Be(1000);
        
        // Проверка лимита в 5 партнеров
        result.Partners.Should().HaveCount(5);
        result.Partners.First().Name.Should().Be("Partner 1");
    }

    [Fact]
    public async Task GetDashboardAsync_ShouldHandleNullUser_ReturningNullName()
    {
        // Arrange
        var userId = 1;
        _userMock.GetUserInternal(userId).Returns((User?)null);
        _partnerMock.GetSortedPartnersAsync(userId).Returns(new List<PartnerResponse>());

        // Act
        var result = await _service.GetDashboardAsync(userId);

        // Assert
        result.UserName.Should().BeNull();
    }

    [Fact]
    public async Task GetDashboardAsync_ShouldReturnFewerThanFivePartners_WhenListIsShort()
    {
        // Arrange
        var userId = 1;
        var shortPartners = new List<PartnerResponse> { new() { Name = "Solo Partner" } };
        _partnerMock.GetSortedPartnersAsync(userId).Returns(shortPartners);

        // Act
        var result = await _service.GetDashboardAsync(userId);

        // Assert
        result.Partners.Should().HaveCount(1);
        result.Partners.First().Name.Should().Be("Solo Partner");
    }

    [Fact]
    public async Task GetDashboardAsync_ShouldPropagateException_WhenServiceFails()
    {
        // Arrange
        var userId = 1;
        _userMock.GetUserInternal(userId).ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var act = async () => await _service.GetDashboardAsync(userId);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Database connection failed");
    }
}