using Domain.Entities;
using Domain.Enums;

namespace Application.Models;

public class TransactionResponse
{
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public MerchantCategory Category { get; set; }
    public bool IsPartner { get; set; }
}

public static class TransactionMapper
{
    public static TransactionResponse ToResponse(this Transaction transaction)
    {
        return new TransactionResponse
        {
            AccountId = transaction.AccountId,
            Amount = transaction.Amount,
            TransactionDate = transaction.TransactionDate,
            Category = transaction.Category,
            IsPartner = transaction.IsPartner,
        };
    }
}