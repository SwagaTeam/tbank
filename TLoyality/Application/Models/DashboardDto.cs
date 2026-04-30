namespace Application.Models;

public class DashboardDto
{
    public string? UserName { get; set; }
    public LoyaltyAnalyticsDto LoyaltyAnalytics { get; set; }
    public ICollection<PartnerResponse> Partners { get; set; }
    public string? AiMessage { get; set; }
}