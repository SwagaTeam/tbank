using Application.Services.Abstractions;
using Application.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<ICsvImportService, CsvImportService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPartnerService, PartnerService>();
        services.AddScoped<IShadowModeService, ShadowModeService>();        
        services.AddScoped<ICrossSellService, CrossSellService>();
        services.AddScoped<ITransactionService, TransactionService>();

        return services;
    }
}