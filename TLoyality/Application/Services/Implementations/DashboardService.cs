using Application.Models;
using Application.Services.Abstractions;

namespace Application.Services.Implementations;

public class DashboardService(
    ILoyaltyService loyaltyService,
    IUserService userService,
    IPartnerService partnerService,
    IShadowModeService aiService)
    : IDashboardService
{
    public async Task<DashboardDto> GetDashboardAsync(int userId)
    {
        var user = await userService.GetUserInternal(userId);
        var loyalty = await loyaltyService.GetUserLoyaltySummaryAsync(userId, false);
        var partners = await partnerService.GetSortedPartnersAsync(userId);
        
        var aiMessage = await aiService.GetQuickSavingsHighlightAsync(userId);

        return new DashboardDto
        {
            UserName = user?.FullName,
            LoyaltyAnalytics = loyalty,
            Partners = partners.Take(5).ToList(),
            AiMessage = aiMessage
        };
    }
}