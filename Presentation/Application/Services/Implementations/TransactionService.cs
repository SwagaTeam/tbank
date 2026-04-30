using Application.Models;
using Application.Services.Abstractions;
using Domain.Entities;
using Infrastructure.Repositories.Abstractions;

namespace Application.Services.Implementations;

public class TransactionService(ITransactionRepository repository) 
    : ITransactionService
{
    const decimal BonusStep = 0.5m; 
    const int DaysInPeriod = 7;

    public async Task<ICollection<TransactionResponse>> GetByAccountIdsAsync(ICollection<int> accountIds)
    {
        var result = await repository.GetByAccountIdsAsync(accountIds);
        return result
            .Select(x => x.ToResponse())
            .ToList();
    }
    /// <summary>
    /// Возвращает максимальное количество транзакций, идущих подряд (день за днем).
    /// Если между транзакциями пропуск более чем в 1 день, счетчик сбрасывается.
    /// </summary>
    public async Task<int> GetConsecutiveTransactionsCount(int accountId)
    {
        var transactions = await repository.GetByAccountIdsAsync(new List<int> { accountId });

        if (transactions.Count == 0)
            return 0;

        var sortedTransactions = transactions
            .OrderBy(t => t.TransactionDate)
            .ToList();

        var maxStreak = 1;
        var currentStreak = 1;

        for (var i = 1; i < sortedTransactions.Count; i++)
        {
            var previousDate = sortedTransactions[i - 1].TransactionDate;
            var currentDate = sortedTransactions[i].TransactionDate;

            var dayDifference = currentDate.DayNumber - previousDate.DayNumber;

            switch (dayDifference)
            {
                case 1:
                    currentStreak++;
                    break;
                case > 1:
                    currentStreak = 1;
                    break;
            }

            if (currentStreak > maxStreak)
            {
                maxStreak = currentStreak;
            }
        }

        return maxStreak;
    }
    
    /// <summary>
    /// Рассчитывает общую надбавку к кешбэку на основе серии транзакций.
    /// За каждые полные 7 дней подряд начисляется +0.5%.
    /// </summary>
    public async Task<decimal> GetCashbackBonusRate(int accountId)
    {
        var consecutiveDays = await GetConsecutiveTransactionsCount(accountId);
        var fullPeriods = consecutiveDays / DaysInPeriod;
        return fullPeriods * BonusStep;
    }
}