using Domain.Enums;
using Application.Services.Abstractions;
using Domain.Entities;

namespace Application.Services.Implementations;

/// <summary>
/// Сервис для формирования персонализированных предложений кросс-продаж на основе сегментации пользователей.
/// </summary>
public class CrossSellService(IUserService userService) : ICrossSellService
{
    private readonly List<CrossSellProduct> _catalog =
    [
        new()
        {
            Name = "Т-Бизнес",
            TargetSegment = FinancialSegment.HIGH,
            Description = "Открой счет для бизнеса с бонусом 3 месяца"
        },


        new()
        {
            Name = "Т-Инвестиции",
            TargetSegment = FinancialSegment.MEDIUM,
            Description = "Начни инвестировать: акция в подарок за обучение"
        },


        new()
        {
            Name = "Т-Мобайл",
            TargetSegment = FinancialSegment.LOW,
            Description = "Перенеси номер и получи 1000 ₽ на счет"
        }
    ];

    /// <summary>
    /// Получает список доступных продуктов для конкретного пользователя, исходя из его финансового сегмента.
    /// </summary>
    /// <param name="userId">Уникальный идентификатор пользователя.</param>
    /// <returns>Коллекция подходящих продуктов для кросс-продаж.</returns>
    public async Task<IEnumerable<CrossSellProduct>> GetPersonalizedOffersAsync(int userId)
    {
        var user = await userService.GetUserInternal(userId);

        return _catalog.Where(x => x.TargetSegment == user?.FinancialSegment);
    }
}