using Application.Models;

namespace Application.Services.Abstractions;

public interface IPartnerService
{
    Task<ICollection<PartnerResponse>> GetSortedPartnersAsync(int userId);
}