using Application.Models;

namespace Application.Services.Abstractions;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(int userId);
}