using System.Globalization;
using System.Runtime.Serialization;
using Application.Services.Abstractions;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Domain;
using Infrastructure.Repositories.Abstractions; 
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.Implementations;

public class CsvImportService(IServiceProvider serviceProvider) : ICsvImportService
{
    public async Task<int> ImportAsync<T>(IFormFile? file) where T : class
    {
        if (file == null || file.Length == 0) return 0;

        var repository = serviceProvider.GetRequiredService<IRepository<T>>();

        using var reader = new StreamReader(file.OpenReadStream());
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.Replace("_", "").ToLower(),
            HeaderValidated = null, 
            MissingFieldFound = null
        };

        using var csv = new CsvReader(reader, config);
    
        csv.Context.TypeConverterCache.AddConverter<LoyaltyProgramName>(new EnumMemberConverter<LoyaltyProgramName>());
        csv.Context.TypeConverterCache.AddConverter<CashbackCurrency>(new EnumMemberConverter<CashbackCurrency>());
        var records = csv.GetRecords<T>().ToList();

        if (records.Count <= 0) return records.Count;
        await repository.AddRangeAsync(records);
        await repository.SaveChangesAsync();

        return records.Count;
    }
}

public class EnumMemberConverter<T> : EnumConverter where T : struct, Enum
{
    public EnumMemberConverter() : base(typeof(T)) { }

    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return default(T);

        foreach (var field in typeof(T).GetFields())
        {
            if (Attribute.GetCustomAttribute(field, typeof(EnumMemberAttribute)) is EnumMemberAttribute attribute)
            {
                if (attribute.Value == text) return field.GetValue(null);
            }
            if (field.Name == text) return field.GetValue(null);
        }

        return base.ConvertFromString(text, row, memberMapData);
    }
}
