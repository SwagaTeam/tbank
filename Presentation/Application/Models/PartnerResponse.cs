using Domain;

namespace Application.Models;

public class PartnerResponse
{
    /// <summary>
    /// Официальное наименование бренда партнера.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Краткое описание условий акции или категории товаров.
    /// </summary>
    /// <example>На все покупки при оплате картой</example>
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>
    /// Прямая ссылка на изображение логотипа партнера.
    /// </summary>
    public string LogoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Фирменный цвет бренда в формате HEX-кода. 
    /// </summary>
    /// <example>#5E9E2C</example>
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Процент кешбэка, доступный пользователю у данного партнера.
    /// </summary>
    /// <example>7.5</example>
    public decimal CashbackPercent { get; set; }
}

public static class PartnerMapper
{
    public static PartnerResponse ToResponse(this Offers offer)
    {
        return new PartnerResponse
        {
            Name = offer.Name,
            ShortDescription = offer.ShortDescription,
            LogoUrl = offer.LogoUrl,
            Color = offer.BrandColorHex,
            CashbackPercent = offer.CashbackPercent
        };
    }
}