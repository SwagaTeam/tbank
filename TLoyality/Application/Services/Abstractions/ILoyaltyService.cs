using Application.Models;

namespace Application.Services.Abstractions;

public interface ILoyaltyService
{
    public Task<LoyaltyAnalyticsDto> GetUserLoyaltySummaryAsync(int userId, bool includeMonthlyHistory = true);
    public Task<ShadowPromptContext> GetShadowContext(int userId);
}
