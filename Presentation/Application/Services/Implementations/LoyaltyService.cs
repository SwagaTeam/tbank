using System.Globalization;
using Application.Models;
using Application.Services.Abstractions;
using Domain;
using Infrastructure.Repositories.Abstractions;

namespace Application.Services.Implementations;

internal class LoyaltyService(
    IAccountRepository accountRepository,
    ILoyaltyHistoryRepository historyRepository,
    IRepository<LoyaltyPrograms> programRepository,
    IOfferRepository offerRepository,
    IUserService userService)
    : ILoyaltyService
{
    public async Task<LoyaltyAnalyticsDto> GetUserLoyaltySummaryAsync(int userId)
    {
        var userAccounts = await accountRepository.GetByUserIdAsync(userId);
        if (userAccounts.Count == 0)
            return new LoyaltyAnalyticsDto();

        var accountIds = userAccounts.Select(a => a.Id).ToList();
        var userHistory = await historyRepository.GetByAccountIdsAsync(accountIds);
        var allPrograms = await programRepository.GetAllAsync();
        var transactions = await transactionRepository.GetByAccountIdsAsync(accountIds);

        var result = new LoyaltyAnalyticsDto();

        // 1. Мапим балансы и историю (старая логика)
        MapBalances(result, userAccounts, userHistory, allPrograms);
        result.MonthlyHistory = MapMonthlyHistory(userAccounts, userHistory, allPrograms);

        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        // 2. Сколько заработали за ТЕКУЩИЙ месяц
        result.CurrentMonthEarned = userHistory
            .Where(h => h.PayoutDate.Month == now.Month && h.PayoutDate.Year == now.Year)
            .Sum(h => h.CashbackAmount);

        // 3. Данные для графика за 9 месяцев (Labels + Values)
        var culture = new CultureInfo("ru-RU");
        for (int i = 8; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var label = monthDate.ToString("MMMM", culture);
            var value = (decimal)userHistory
                .Where(h => h.PayoutDate.Month == monthDate.Month && h.PayoutDate.Year == monthDate.Year)
                .Sum(h => h.CashbackAmount);

            result.Last9MonthsLabels.Add(label);
            result.Last9MonthsValues.Add(value);
        }

        // 4. Прогноз на 3 месяца (берем средние траты за 30 дней и считаем 1% кешбэка)
        var thirtyDaysAgo = now.AddDays(-30);
        var lastMonthSpend = transactions
            .Where(t => t.TransactionDate >= thirtyDaysAgo)
            .Sum(t => t.Amount);

        var averageDailySpend = lastMonthSpend / 30m;
        result.PredictedBenefit3Months = Math.Round(averageDailySpend * 90m * 0.01m, 2);

        // 5. Анализ топ-категории для рекомендации
        var topCategory = transactions
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Total)
            .FirstOrDefault();

        if (topCategory != null)
        {
            result.RecommendedCategoryName = topCategory.Category.ToString();
            result.PotentialCategorySavings = Math.Round(topCategory.Total * 0.05m, 2); // 5% повышенный кешбэк
        }

        // 6. Траты у партнеров
        result.TotalPartnerSpend = transactions
            .Where(t => t.IsPartner)
            .Sum(t => t.Amount);

        return result;
    }

    public async Task<ShadowPromptContext> GetShadowContext(int userId)
    {
        var userAccounts = await accountRepository.GetByUserIdAsync(userId);
        var user = await userService.GetUser(userId);
        if (user is null)
        {
            throw new UnauthorizedAccessException();
        }
        
        var accountIds = userAccounts.Select(a => a.Id);
        var userHistory = await historyRepository.GetByAccountIdsAsync(accountIds);
        var allPrograms = await programRepository.GetAllAsync();
        var offer = await offerRepository.GetPartnersAsync(user.FinancialSegment);

        return new ShadowPromptContext
        (
            User: user,
            CurrentAccount: userAccounts,
            RecentHistory: userHistory,
            AvailablePrograms: allPrograms,
            RelevantOffers: offer
        );
    }

    private void MapBalances(
        LoyaltyAnalyticsDto result, 
        ICollection<Accounts> userAccounts, 
        ICollection<LoyaltyHistory> userHistory, 
        ICollection<LoyaltyPrograms> allPrograms)
    {
        foreach (var account in userAccounts)
        {
            var program = allPrograms.First(p => p.LoyaltyProgramId == account.LoyaltyProgramId);
            
            var totalPaid = userHistory
                .Where(h => h.AccountId == account.Id)
                .Sum(h => h.CashbackAmount);

            switch (program.LoyaltyProgramName)
            {
                case LoyaltyProgramName.Black: result.TotalRub += totalPaid; break; 
                case LoyaltyProgramName.AllAirlines: result.TotalMiles += totalPaid; break; 
                case LoyaltyProgramName.Bravo: result.TotalBravo += totalPaid; break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private IList<HistoryPointDto> MapMonthlyHistory(
        ICollection<Accounts> userAccounts, 
        ICollection<LoyaltyHistory> userHistory, 
        ICollection<LoyaltyPrograms> allPrograms)
    {
        return userHistory
            .OrderBy(h => h.PayoutDate)
            .Select(h => 
            {
                var account = userAccounts.First(a => a.Id == h.AccountId);
                var program = allPrograms.First(p => p.LoyaltyProgramId == account.LoyaltyProgramId);
                
                return new HistoryPointDto(
                    h.PayoutDate, 
                    h.CashbackAmount, 
                    program.CashbackCurrency.ToString()
                );
            })
            .ToList();
    }

    private decimal CalculateForecast(ICollection<LoyaltyHistory> userHistory)
    {
        if (userHistory.Count == 0)
        {
            return 0;
        }

        var recentValues = userHistory
            .OrderByDescending(h => h.PayoutDate)
            .Take(3)
            .Select(h => h.CashbackAmount)
            .ToList();

        if (recentValues.Count == 0)
        {
            return 0;
        }

        var averageRecent = (decimal)recentValues.Average();
        
        return Math.Round(averageRecent * 1.05m, 2);
    }
}