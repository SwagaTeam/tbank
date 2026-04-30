namespace Domain.Entities;

public class Accounts
{
    public int AccountId { get; set; }
    public int UserId { get; set; }
    public int LoyaltyProgramId { get; set; }
    public decimal CurrentBalance { get; set; }
}