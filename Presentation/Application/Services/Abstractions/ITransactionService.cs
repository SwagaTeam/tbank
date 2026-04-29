using Transaction = Domain.Entities.Transaction;

namespace Application.Services.Abstractions;

public interface ITransactionService
{
    Task<int> GetConsecutiveTransactionsCount(int userId);
}