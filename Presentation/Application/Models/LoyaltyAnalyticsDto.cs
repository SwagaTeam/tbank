namespace Application.Models;

public class LoyaltyAnalyticsDto
{
    // Балансы по программам
    public decimal TotalRub { get; set; }
    public decimal TotalMiles { get; set; }
    public decimal TotalBravo { get; set; }

    // Старая история (точки для графика, если нужны)
    public IList<HistoryPointDto> MonthlyHistory { get; set; } = new List<HistoryPointDto>();

    // НОВЫЕ ПОЛЯ
    public decimal CurrentMonthEarned { get; set; } // Заработок за текущий месяц

    // Данные для графика за 9 месяцев
    public List<string> Last9MonthsLabels { get; set; } = new();
    public List<decimal> Last9MonthsValues { get; set; } = new();

    public decimal PredictedBenefit3Months { get; set; } // Прогноз на 3 месяца

    public string RecommendedCategoryName { get; set; } = string.Empty; // Название топ-категории
    public decimal PotentialCategorySavings { get; set; } // Сколько можно было бы сэкономить (5%)

    public decimal TotalPartnerSpend { get; set; } // Траты у партнеров
}

public record HistoryPointDto(DateOnly Date, decimal Amount, string Currency);