namespace Domain;

public class Accounts
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LoyaltyProgramId { get; set; }
    public decimal CurrentBalance { get; set; }
}