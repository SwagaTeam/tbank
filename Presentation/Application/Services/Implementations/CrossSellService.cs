using Application.Services.Abstractions;
using Domain;
using Domain.Entities;

namespace Application.Services.Implementations
{
    public class CrossSellService : ICrossSellService
    {
        private readonly List<CrossSellProduct> _catalog = new()
        {
            new() { Name = "Т-Бизнес", TargetSegment = FinancialSegment.HIGH, Description = "Открой ИП бесплатно" },
            new() { Name = "Т-Инвестиции", TargetSegment = FinancialSegment.MEDIUM, Description = "Премиальный сервис для акций" },
            new() { Name = "Т-Мобайл", TargetSegment = FinancialSegment.LOW, Description = "Первый месяц связи бесплатно" }
        };

        public IEnumerable<CrossSellProduct> GetOffers(FinancialSegment segment)
        {
            // Базовая логика: предлагаем то, что подходит под сегмент + "общие" предложения
            return _catalog.Where(x => x.TargetSegment == segment);
        }
    }
}
