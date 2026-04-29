using Domain;
using Infrastructure.Repositories.Abstractions;
using Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ILoyaltyHistoryRepository, LoyaltyHistoryRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRepository<LoyaltyPrograms>, Repository<LoyaltyPrograms>>();

        return services;
    }
}