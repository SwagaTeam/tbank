using Application.Models;
using Domain;

namespace Application.Services.Abstractions;

public interface IPartnerService
{
    Task<ICollection<PartnerResponse>> GetSortedPartnersAsync(int userId);
}