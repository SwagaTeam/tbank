using Domain;

namespace Application.Models;

public class PartnerResponse
{
    public string Name { get; set; }
    public string ShortDescription { get; set; }
    public string LogoUrl { get; set; }
    public string Color { get; set; }
    public decimal CashbackPercent { get; set; }
}

public static class PartnerMapper
{
    public static PartnerResponse ToResponse(this Offers offer)
    {
        return new PartnerResponse
        {
            Name = offer.PartnerName,
            ShortDescription = offer.ShortDescription,
            LogoUrl = offer.LogoUrl,
            Color = offer.BrandColorHex,
            CashbackPercent = offer.CashbackPercent
        };
    }
}