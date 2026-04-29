namespace Domain;

public class LoyaltyHistory
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int CashbackAmount { get; set; }
    public DateOnly PayoutDate { get; set; }
}