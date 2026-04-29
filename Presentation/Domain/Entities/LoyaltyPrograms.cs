namespace Domain;

public class LoyaltyPrograms
{
    public Guid Id { get; set; }
    public LoyaltyProgramName Name { get; set; }
    public CashbackCurrency Currency { get; set; }
}