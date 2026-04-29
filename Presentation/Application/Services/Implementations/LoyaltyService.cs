using Application.Models;
using Application.Services.Abstractions;
using Domain;
using Infrastructure.Repositories.Abstractions;

namespace Application.Services.Implementations;

public class LoyaltyService(
    IAccountRepository accountRepository,
    ILoyaltyHistoryRepository historyRepository,
    IRepository<LoyaltyPrograms> programRepository)
    : ILoyaltyService
{
    public async Task<LoyaltyAnalyticsDto> GetUserLoyaltySummaryAsync(Guid userId)
    {
        var userAccounts = await accountRepository.GetByUserIdAsync(userId);
        if (userAccounts.Count == 0)
        {
            return new LoyaltyAnalyticsDto();
        }

        var accountIds = userAccounts.Select(a => a.Id);
        var userHistory = await historyRepository.GetByAccountIdsAsync(accountIds);

        var allPrograms = await programRepository.GetAllAsync();

        var result = new LoyaltyAnalyticsDto();

        MapBalances(result, userAccounts, userHistory, allPrograms);
        
        result.MonthlyHistory = MapMonthlyHistory(userAccounts, userHistory, allPrograms);
        
        result.PredictedBenefitNextMonth = CalculateForecast(userHistory);

        return result;
    }

    private void MapBalances(
        LoyaltyAnalyticsDto result, 
        ICollection<Accounts> userAccounts, 
        ICollection<LoyaltyHistory> userHistory, 
        ICollection<LoyaltyPrograms> allPrograms)
    {
        foreach (var account in userAccounts)
        {
            var program = allPrograms.First(p => p.Id == account.LoyaltyProgramId);
            
            var totalPaid = userHistory
                .Where(h => h.AccountId == account.Id)
                .Sum(h => h.CashbackAmount);

            switch (program.Name)
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
                var program = allPrograms.First(p => p.Id == account.LoyaltyProgramId);
                
                return new HistoryPointDto(
                    h.PayoutDate, 
                    h.CashbackAmount, 
                    program.Currency.ToString()
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