using System.Globalization;
using Application.Services.Abstractions;
using CsvHelper;
using CsvHelper.Configuration;
using Infrastructure.Repositories.Abstractions; 
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.Implementations;

public class CsvImportService(IServiceProvider serviceProvider) : ICsvImportService
{
    public async Task<int> ImportAsync<T>(IFormFile? file) where T : class
    {
        if (file == null || file.Length == 0)
        {
            return 0;
        }

        var repository = serviceProvider.GetRequiredService<IRepository<T>>();

        using var reader = new StreamReader(file.OpenReadStream());
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.Replace("_", "").ToLower(),
            HeaderValidated = null, 
            MissingFieldFound = null
        };

        using var csv = new CsvReader(reader, config);
        
        var records = csv.GetRecords<T>().ToList();

        if (records.Count <= 0)
        {
            return records.Count;
        }
        
        await repository.AddRangeAsync(records);
        await repository.SaveChangesAsync();

        return records.Count;
    }
}