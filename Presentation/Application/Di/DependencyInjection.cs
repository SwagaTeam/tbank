using Application.Services.Abstractions;
using Application.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Di;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<ICsvImportService, CsvImportService>();
        
        return services;
    }
}