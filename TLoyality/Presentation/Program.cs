using System.Reflection;
using Application;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Настройка сервера
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AllowAlternateSchemes = true;
        });

        // Регистрация слоев и сервисов
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services
            .AddHttpClient()
            .AddInfrastructure(connectionString)
            .AddApplication()
            .AddEndpointsApiExplorer()
            .AddControllers();

        builder.Services.AddScoped<DbInitializer>();
        
        AddSwagger(builder.Services);
        
        var app = builder.Build();

        // --- ИСПРАВЛЕНИЕ ДЛЯ ТЕСТОВ ---
        // Запускаем миграции только если мы НЕ в режиме тестирования
        if (!app.Environment.IsEnvironment("Testing"))
        {
            using var scope = app.Services.CreateScope();
            try
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Проверяем наличие строки подключения перед миграцией
                if (!string.IsNullOrEmpty(connectionString))
                {
                    await db.Database.MigrateAsync();
                    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
                    await initializer.SeedAsync();
                }
            }
            catch (Exception ex)
            {
                // В реальном проекте здесь должен быть логгер
                Console.WriteLine($"Ошибка при инициализации БД: {ex.Message}");
            }
        }

        // Middleware пайплайн
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.UseHttpsRedirection();
        app.MapControllers();

        await app.RunAsync();
    }

    static void AddSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Loyalty API",
                Version = "v1",
                Description = "API для платформы лояльности и персонализированных офферов"
            });

            // Безопасное добавление XML-комментариев
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });
    }
}

// Критически важно для WebApplicationFactory в тестах!
public partial class Program { }