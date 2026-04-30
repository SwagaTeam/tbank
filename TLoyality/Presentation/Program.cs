using System.Reflection;
using Application;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()  
                    .AllowAnyMethod()   
                    .AllowAnyHeader(); 
            });
        });
        
        services
            .AddHttpClient()
            .AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection"))
            .AddApplication()
            .AddEndpointsApiExplorer()
            .AddControllers();
        
        AddSwagger(services);
        
        var app = builder.Build();

        app.UseCors("AllowAll");

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();

        app.UseHttpsRedirection();

        app.MapControllers();

        app.Run();
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