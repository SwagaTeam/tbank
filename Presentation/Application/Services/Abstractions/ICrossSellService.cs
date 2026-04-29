using Domain;
using Domain.Entities;

namespace Application.Services.Abstractions
{
    public interface ICrossSellService
    {
        Task<IEnumerable<CrossSellProduct>> GetPersonalizedOffersAsync(int userId);
    }
}
