using Application.Services.Abstractions;
using Domain.Entities;
using Infrastructure.Repositories.Abstractions;

namespace Application.Services.Implementations;

public class TransactionService(ITransactionRepository repository, IAccountRepository accountRepository) 
    : ITransactionService
{
    /// <summary>
    /// Возвращает максимальное количество транзакций, идущих подряд (день за днем).
    /// Если между транзакциями пропуск более чем в 1 день, счетчик сбрасывается.
    /// </summary>
    public async Task<int> GetConsecutiveTransactionsCount(int userId)
    {
        var accounts = await accountRepository.GetByUserIdAsync(userId);
        var transactions = await repository.GetByAccountIdsAsync(accounts.Select(x=>x.AccountId));
        
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

    public int GetConsecutiveTransactionsCount(ICollection<System.Transactions.Transaction> transactions)
    {
        throw new NotImplementedException();
    }
}