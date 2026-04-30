using Application.Services.Abstractions;
using Application.Services.Implementations;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Repositories.Abstractions;
using NSubstitute;
using Xunit;

namespace TLoyality.Tests.Tests;

public class PartnerServiceTests
{
    private readonly IOfferRepository _repositoryMock = Substitute.For<IOfferRepository>();
    private readonly IUserService _userServiceMock = Substitute.For<IUserService>();
    private readonly PartnerService _service;

    public PartnerServiceTests()
    {
        _service = new PartnerService(_repositoryMock, _userServiceMock);
    }

    /// <summary>
    /// Позитивный кейс: Успешное получение списка партнеров для существующего пользователя.
    /// </summary>
    [Fact]
    public async Task GetSortedPartnersAsync_ShouldReturnPartnerResponses_WhenUserExists()
    {
        // Arrange
        var userId = 777;
        var segment = FinancialSegment.HIGH;
        var user = new User { Id = userId, FinancialSegment = segment };
        
        var offers = new List<Offers>
        {
            new() 
            { 
                PartnerName = "Tesla", 
                ShortDescription = "Eco driving", 
                CashbackPercent = 10, 
                BrandColorHex = "#000000",
                LogoUrl = "tesla.png"
            },
            new() 
            { 
                PartnerName = "SpaceX", 
                ShortDescription = "Fly high", 
                CashbackPercent = 15, 
                BrandColorHex = "#FFFFFF",
                LogoUrl = "spacex.png"
            }
        };

        _userServiceMock.GetUserInternal(userId).Returns(user);
        _repositoryMock.GetPartnersAsync(segment).Returns(offers);

        // Act
        var result = await _service.GetSortedPartnersAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainSingle(x => x.Name == "Tesla" && x.CashbackPercent == 10);
        result.Should().ContainSingle(x => x.Name == "SpaceX" && x.CashbackPercent == 15);
        
        await _repositoryMock.Received(1).GetPartnersAsync(segment);
    }

    /// <summary>
    /// Негативный кейс: Если пользователь не найден, должно выбрасываться исключение UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task GetSortedPartnersAsync_ShouldThrowUnauthorized_WhenUserNotFound()
    {
        // Arrange
        int nonExistentUserId = 999;
        _userServiceMock.GetUserInternal(nonExistentUserId).Returns((User)null!);

        // Act
        var act = () => _service.GetSortedPartnersAsync(nonExistentUserId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _repositoryMock.DidNotReceiveWithAnyArgs().GetPartnersAsync(default);
    }

    /// <summary>
    /// Пограничный случай: У пользователя нет доступных партнеров для его сегмента.
    /// </summary>
    [Fact]
    public async Task GetSortedPartnersAsync_ShouldReturnEmptyList_WhenNoOffersInSegment()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, FinancialSegment = FinancialSegment.MEDIUM };
        
        _userServiceMock.GetUserInternal(userId).Returns(user);
        _repositoryMock.GetPartnersAsync(FinancialSegment.MEDIUM).Returns(new List<Offers>());

        // Act
        var result = await _service.GetSortedPartnersAsync(userId);

        // Assert
        result.Should().BeEmpty();
        await _repositoryMock.Received(1).GetPartnersAsync(FinancialSegment.MEDIUM);
    }

    /// <summary>
    /// Проверка корректности маппинга: Поля из доменной модели должны правильно переходить в Response.
    /// </summary>
    [Fact]
    public async Task GetSortedPartnersAsync_ShouldCorrectlyMapFields()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, FinancialSegment = FinancialSegment.HIGH };
        var offer = new Offers
        {
            PartnerName = "Apple",
            ShortDescription = "Tech",
            LogoUrl = "apple.logo",
            BrandColorHex = "#FFFFFF",
            CashbackPercent = 5.5m
        };

        _userServiceMock.GetUserInternal(userId).Returns(user);
        _repositoryMock.GetPartnersAsync(FinancialSegment.HIGH).Returns(new List<Offers> { offer });

        // Act
        var result = await _service.GetSortedPartnersAsync(userId);
        var partner = result.First();

        // Assert
        partner.Name.Should().Be(offer.PartnerName);
        partner.ShortDescription.Should().Be(offer.ShortDescription);
        partner.LogoUrl.Should().Be(offer.LogoUrl);
        partner.Color.Should().Be(offer.BrandColorHex);
        partner.CashbackPercent.Should().Be(offer.CashbackPercent);
    }
}