using Application.Models;
using Application.Services.Abstractions;
using Domain;
using Infrastructure.Repositories.Abstractions;

namespace Application.Services.Implementations;

public class PartnerService(IOfferRepository repository, IUserService userService) : IPartnerService
{
    public async Task<ICollection<PartnerResponse>> GetSortedPartnersAsync(int userId)
    {
        var segment = await userService.GetSegment(userId);
        if (segment is null)
        {
            throw new UnauthorizedAccessException();
        }
        
        var result = await repository.GetPartnersAsync(segment.Value);
        return result
            .Select(x => x.ToResponse())
            .ToList();
    }
}