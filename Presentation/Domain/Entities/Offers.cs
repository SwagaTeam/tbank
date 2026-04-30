using Domain.Enums;

namespace Domain.Entities;

public class Offers
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string BrandColorHex { get; set; } = string.Empty;
    public decimal CashbackPercent { get; set; }
    public FinancialSegment FinancialSegment { get; set; }
}