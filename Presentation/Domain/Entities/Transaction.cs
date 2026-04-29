using Domain.Enums;

namespace Domain.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
        public DateOnly TransactionDate { get; set; }
        public MerchantCategory Category { get; set; }
        public bool IsPartner { get; set; }
    }
}
