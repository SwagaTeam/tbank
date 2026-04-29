namespace Application.Models;

public class LoyaltyAnalyticsDto
{
    public decimal TotalRub { get; set; }
    public int TotalMiles { get; set; }
    public int TotalBravo { get; set; }

    public decimal PredictedBenefitNextMonth { get; set; }

    public ICollection<HistoryPointDto> MonthlyHistory { get; set; }
}

public record HistoryPointDto(DateOnly Date, decimal Amount, string Currency);