using Application.Models;
using Domain;

namespace Application.Services.Abstractions;

public interface ILoyaltyService
{
    public Task<LoyaltyAnalyticsDto> GetUserLoyaltySummaryAsync(int userId);
}