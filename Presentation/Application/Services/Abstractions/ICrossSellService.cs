using Domain;
using Domain.Entities;

namespace Application.Services.Abstractions
{
    public interface ICrossSellService
    {
       IEnumerable<CrossSellProduct> GetOffers(FinancialSegment segment);
    }
}
