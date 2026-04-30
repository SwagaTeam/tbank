using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application
{
    /// <summary>
    /// Автоматически наполняет БД при старте приложения, если она пуста.
    /// </summary>
    public class DbInitializer(IServiceScopeFactory scopeFactory, ILogger<DbInitializer> logger)
    {
        public async Task SeedAsync()
        {
            using var scope = scopeFactory.CreateScope();
            var csvService = scope.ServiceProvider.GetRequiredService<ICsvImportService>();
            var context = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();

            if (await context.Users.AnyAsync())
            {
                logger.LogInformation("База данных уже содержит данные. Пропуск инициализации.");
                return;
            }

            var baseDir = AppContext.BaseDirectory;
            var seedPath = Path.Combine(baseDir, "SeedData");

            if (!Directory.Exists(seedPath))
            {
                seedPath = "/app/SeedData";
            }

            logger.LogInformation("Итоговый путь для поиска CSV: {Path}", seedPath);

            if (!Directory.Exists(seedPath))
            {
                logger.LogError("Папка SeedData не найдена по пути: {Path}", seedPath);
                return;
            }

            logger.LogInformation("Начало автоматического импорта данных из {Path}...", seedPath);

            try
            {
                int u = await csvService.ImportFromPathAsync<User>(Path.Combine(seedPath, "Users.csv"));
                int p = await csvService.ImportFromPathAsync<LoyaltyPrograms>(Path.Combine(seedPath, "LoyaltyPrograms.csv"));
                int a = await csvService.ImportFromPathAsync<Accounts>(Path.Combine(seedPath, "Accounts.csv"));
                int h = await csvService.ImportFromPathAsync<LoyaltyHistory>(Path.Combine(seedPath, "LoyaltyHistory.csv"));
                int o = await csvService.ImportFromPathAsync<Offers>(Path.Combine(seedPath, "Offers.csv"));

                logger.LogInformation("Импорт завершен: Users:{u}, Programs:{p}, Accounts:{a}, History:{h}, Offers:{o}", u, p, a, h, o);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка во время автоматического импорта данных.");
            }
        }
    }
}
