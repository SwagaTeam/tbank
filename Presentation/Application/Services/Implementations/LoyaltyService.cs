using System.Globalization;
using Application.Models;
using Application.Services.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Abstractions;

namespace Application.Services.Implementations;

internal class LoyaltyService(
    IAccountRepository accountRepository,
    ILoyaltyHistoryRepository historyRepository,
    IRepository<LoyaltyPrograms> programRepository,
    IOfferRepository offerRepository,
    IUserService userService,
    ITransactionService transactionService)
    : ILoyaltyService
{
    public async Task<LoyaltyAnalyticsDto> GetUserLoyaltySummaryAsync(int userId)
    {
        var userAccounts = await accountRepository.GetByUserIdAsync(userId);
        if (userAccounts.Count == 0)
            return new LoyaltyAnalyticsDto();

        var accountIds = userAccounts.Select(a => a.AccountId).ToList();
        
        var totalBonusRate = decimal.Zero;
        foreach (var id in accountIds)
        {
            totalBonusRate += await transactionService.GetCashbackBonusRate(id);
        }

        totalBonusRate /= 100m;
        
        var userHistory = await historyRepository.GetByAccountIdsAsync(accountIds);
        var allPrograms = await programRepository.GetAllAsync();
        var transactions = await transactionService.GetByAccountIdsAsync(accountIds);

        var result = new LoyaltyAnalyticsDto();
        var culture = new CultureInfo("ru-RU");
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        MapBalances(result, userAccounts, userHistory, allPrograms);
        result.MonthlyHistory = MapMonthlyHistory(userAccounts, userHistory, allPrograms);

        result.CurrentMonthEarned = userHistory
            .Where(h => h.PayoutDate.Month == now.Month && h.PayoutDate.Year == now.Year)
            .Sum(h => h.CashbackAmount);

        for (var i = 8; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var label = monthDate.ToString("MMMM", culture);
            var value = userHistory
                .Where(h => h.PayoutDate.Month == monthDate.Month && h.PayoutDate.Year == monthDate.Year)
                .Sum(h => h.CashbackAmount);

            result.Last9MonthsLabels.Add(label);
            result.Last9MonthsValues.Add(value);
        }
        
        var firstDayOfCurrentMonth = new DateOnly(now.Year, now.Month, 1);
        var lastMonthDate = firstDayOfCurrentMonth.AddMonths(-1);
        
        var analysisStart = new DateOnly(lastMonthDate.Year, lastMonthDate.Month, 1);
        var analysisEnd = firstDayOfCurrentMonth.AddDays(-1);

        var lastFullMonthTransactions = transactions
            .Where(t => t.TransactionDate >= analysisStart && t.TransactionDate <= analysisEnd)
            .ToList();

        var lastMonthTotalSpend = lastFullMonthTransactions.Sum(t => t.Amount);
        var totalRate = 0.01m + totalBonusRate;
        result.PredictedBenefit3Months = Math.Round(lastMonthTotalSpend * 3 * totalRate, 2);
        
        var topCategory = lastFullMonthTransactions
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Total)
            .FirstOrDefault();

        if (topCategory != null)
        {
            result.RecommendedCategoryName = topCategory.Category.MapCategoryToRussian();
            var categoryRate = 0.05m + totalBonusRate;
            result.PotentialCategorySavings = Math.Round(topCategory.Total * categoryRate, 2);
        }

        result.TotalPartnerSpend = lastFullMonthTransactions
            .Where(t => t.IsPartner)
            .Sum(t => t.Amount);

        return result;
    }

    public async Task<ShadowPromptContext> GetShadowContext(int userId)
    {
        var userAccounts = await accountRepository.GetByUserIdAsync(userId);
        var user = await userService.GetUserInternal(userId);
    
        if (user is null)
        {
            throw new UnauthorizedAccessException();
        }

        var accountIds = userAccounts.Select(a => a.AccountId).ToList();

        var history = await historyRepository.GetByAccountIdsAsync(accountIds);
        var transactions = await transactionService.GetByAccountIdsAsync(accountIds);
        var programs = await programRepository.GetAllAsync();
        var offers = await offerRepository.GetPartnersAsync(user.FinancialSegment);

        return new ShadowPromptContext
        (
            User: user,
            CurrentAccount: userAccounts,
            RecentHistory: history,
            Transactions: transactions,
            AvailablePrograms: programs,
            RelevantOffers: offers
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
                .Where(h => h.AccountId == account.AccountId)
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
                var account = userAccounts.First(a => a.AccountId == h.AccountId);
                var program = allPrograms.First(p => p.LoyaltyProgramId == account.LoyaltyProgramId);
                
                return new HistoryPointDto(
                    h.PayoutDate, 
                    h.CashbackAmount, 
                    program.CashbackCurrency.ToString()
                );
            })
            .ToList();
    }
}