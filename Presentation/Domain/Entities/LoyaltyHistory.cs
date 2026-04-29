namespace Domain;

public class LoyaltyHistory
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public int CashbackAmount { get; set; }
    public DateOnly PayoutDate { get; set; }
}