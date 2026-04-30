using Application.Models;

namespace Application.Services.Abstractions;

public interface ILoyaltyService
{
    public Task<LoyaltyAnalyticsDto> GetUserLoyaltySummaryAsync(int userId);
    internal Task<ShadowPromptContext> GetShadowContext(int userId);
}
