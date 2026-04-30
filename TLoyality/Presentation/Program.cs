using System.Reflection;
using Application;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AllowAlternateSchemes = true;
        });

        services
            .AddHttpClient()
            .AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection"))
            .AddApplication()
            .AddEndpointsApiExplorer()
            .AddControllers();

        services.AddScoped<DbInitializer>();
        
        AddSwagger(services);
        
        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            //Migrate
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            //Seed
            var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
            await initializer.SeedAsync();
        }

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

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });
    }
}