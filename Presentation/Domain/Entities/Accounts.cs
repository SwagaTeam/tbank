namespace Domain;

public class Accounts
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int LoyaltyProgramId { get; set; }
    public decimal CurrentBalance { get; set; }
}