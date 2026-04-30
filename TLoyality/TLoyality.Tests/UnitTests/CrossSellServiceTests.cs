using Application.Services.Abstractions;
using Application.Services.Implementations;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace TLoyality.Tests.Tests;

public class CrossSellServiceTests
{
    private readonly IUserService _userServiceMock;
    private readonly CrossSellService _service;

    public CrossSellServiceTests()
    {
        _userServiceMock = Substitute.For<IUserService>();
        _service = new CrossSellService(_userServiceMock);
    }

    /// <summary>
    /// Позитивный тест: пользователь с сегментом HIGH должен получить соответствующие продукты.
    /// </summary>
    [Fact]
    public async Task GetPersonalizedOffersAsync_ShouldReturnHighSegmentProducts_WhenUserIsHighSegment()
    {
        // Arrange
        var userId = 1;
        var user = new User { FinancialSegment = FinancialSegment.HIGH };
        _userServiceMock.GetUserInternal(userId).Returns(user);

        // Act
        var result = await _service.GetPersonalizedOffersAsync(userId);
        var crossSellProducts = result.ToList();

        // Assert
        crossSellProducts.Should().NotBeEmpty();
        crossSellProducts.Should().AllSatisfy(p => p.TargetSegment.Should().Be(FinancialSegment.HIGH));
        crossSellProducts.Should().Contain(p => p.Name == "Т-Бизнес");
    }

    /// <summary>
    /// Позитивный тест: проверка сегмента LOW.
    /// </summary>
    [Fact]
    public async Task GetPersonalizedOffersAsync_ShouldReturnLowSegmentProducts_WhenUserIsLowSegment()
    {
        // Arrange
        var userId = 2;
        var user = new User { FinancialSegment = FinancialSegment.LOW };
        _userServiceMock.GetUserInternal(userId).Returns(user);

        // Act
        var result = await _service.GetPersonalizedOffersAsync(userId);

        // Assert
        result.Should().Contain(p => p.Name == "Т-Мобайл");
    }

    /// <summary>
    /// Негативный сценарий: пользователь не найден (null).
    /// </summary>
    [Fact]
    public async Task GetPersonalizedOffersAsync_ShouldReturnEmpty_WhenUserNotFound()
    {
        // Arrange
        var userId = 999;
        _userServiceMock.GetUserInternal(userId).Returns((User)null!);

        // Act
        var result = await _service.GetPersonalizedOffersAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Граничный случай: сегмент пользователя отсутствует в каталоге.
    /// </summary>
    [Fact]
    public async Task GetPersonalizedOffersAsync_ShouldReturnEmpty_WhenNoProductsMatchSegment()
    {
        // Arrange
        var userId = 3;
        var user = new User { FinancialSegment = (FinancialSegment)999 }; 
        _userServiceMock.GetUserInternal(userId).Returns(user);

        // Act
        var result = await _service.GetPersonalizedOffersAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Тест на обработку исключений через NSubstitute.
    /// </summary>
    [Fact]
    public async Task GetPersonalizedOffersAsync_ShouldThrow_WhenUserServiceFails()
    {
        // Arrange
        var userId = 1;
        _userServiceMock.GetUserInternal(userId).ThrowsAsync(new Exception("Database error"));

        // Act
        var act = async () => await _service.GetPersonalizedOffersAsync(userId);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Database error");
    }
}