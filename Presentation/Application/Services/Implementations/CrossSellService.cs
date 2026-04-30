using Domain.Enums;

namespace Application.Services.Implementations;

using Abstractions;
using Domain.Entities;

public class CrossSellService(IUserService userService) : ICrossSellService
{
    private readonly List<CrossSellProduct> _catalog = new()
    {
        // Продукты для HIGH (Премиум)
        new CrossSellProduct
        {
            Name = "Т-Бизнес", 
            TargetSegment = FinancialSegment.HIGH, 
            Description = "Открой счет для бизнеса с бонусом 3 месяца"
        },
        
        // Продукты для MEDIUM (Средний класс)
        new CrossSellProduct {
            Name = "Т-Инвестиции", 
            TargetSegment = FinancialSegment.MEDIUM, 
            Description = "Начни инвестировать: акция в подарок за обучение" },
        
        // Продукты для LOW (Масс-маркет / Студенты)
        new CrossSellProduct
        {
            Name = "Т-Мобайл",
            TargetSegment = FinancialSegment.LOW,
            Description = "Перенеси номер и получи 1000 ₽ на счет"
        },
    };

    public async Task<IEnumerable<CrossSellProduct>> GetPersonalizedOffersAsync(int userId)
    {
        var user = await userService.GetUserInternal(userId);

        return _catalog.Where(x => x.TargetSegment == user?.FinancialSegment);
    }
}