using Application.Models;

namespace Application.Services.Abstractions;

public interface ITransactionService
{
    Task<int> GetConsecutiveTransactionsCount(int accountId);
    Task<decimal> GetCashbackBonusRate(int accountId);
    Task<ICollection<TransactionResponse>> GetByAccountIdsAsync(ICollection<int> accountIds);
}