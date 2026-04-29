namespace Application.Models;

/// <summary>
/// Транспортный объект для передачи агрегированной аналитики лояльности.
/// Содержит балансы, исторические данные для графиков и предиктивные показатели.
/// </summary>
public class LoyaltyAnalyticsDto
{
    /// <summary> 
    /// Общий баланс кэшбэка в рублях (программа T-Black). 
    /// </summary>
    /// <example>1250.50</example>
    public decimal TotalRub { get; set; }

    /// <summary> 
    /// Накопленные авиамили (программа All Airlines). 
    /// </summary>
    /// <example>5400</example>
    public decimal TotalMiles { get; set; }

    /// <summary> 
    /// Баллы программы лояльности 'Браво'. 
    /// </summary>
    /// <example>320</example>
    public decimal TotalBravo { get; set; }

    /// <summary> 
    /// Детализированная история начислений по датам. 
    /// </summary>
    public IList<HistoryPointDto> MonthlyHistory { get; set; } = new List<HistoryPointDto>();

    /// <summary> 
    /// Сумма кэшбэка, начисленная или ожидаемая к начислению за текущий календарный месяц. 
    /// </summary>
    /// <example>450.00</example>
    public decimal CurrentMonthEarned { get; set; }

    /// <summary> 
    /// Метки осей для графика за последние 9 месяцев (например, сокращенные названия месяцев). 
    /// </summary>
    /// <example>["Сент", "Окт", "Ноя"]</example>
    public List<string> Last9MonthsLabels { get; set; } = new();

    /// <summary> 
    /// Значения кэшбэка для графика за последние 9 месяцев. 
    /// Количество элементов должно совпадать с Last9MonthsLabels.
    /// </summary>
    /// <example>[120.5, 300.0, 150.2]</example>
    public List<decimal> Last9MonthsValues { get; set; } = new();

    /// <summary> 
    /// Прогноз выгоды на ближайшие 3 месяца. 
    /// Рассчитывается на основе среднего чека и паттернов потребления пользователя.
    /// </summary>
    /// <example>1500.00</example>
    public decimal PredictedBenefit3Months { get; set; }

    /// <summary> 
    /// Название категории с максимальными тратами, рекомендуемой к активации в следующем месяце. 
    /// </summary>
    /// <example>Супермаркеты</example>
    public string RecommendedCategoryName { get; set; } = string.Empty;

    /// <summary> 
    /// Сумма потенциальной экономии в рекомендуемой категории при условии повышенного кэшбэка 5%. 
    /// </summary>
    /// <example>850.00</example>
    public decimal PotentialCategorySavings { get; set; }

    /// <summary> 
    /// Суммарный объем трат, совершенных в магазинах-партнерах. 
    /// Используется для оценки вовлеченности в экосистему.
    /// </summary>
    /// <example>45000.00</example>
    public decimal TotalPartnerSpend { get; set; }
}

/// <summary>
/// Точка данных в истории начислений лояльности.
/// </summary>
/// <param name="Date">Дата записи.</param>
/// <param name="Amount">Сумма начисления.</param>
/// <param name="Currency">Валюта или тип баллов (RUB, Miles, Bravo).</param>
public record HistoryPointDto(DateOnly Date, decimal Amount, string Currency);