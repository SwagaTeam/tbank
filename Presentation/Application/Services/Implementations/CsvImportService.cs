using System.Globalization;
using System.Runtime.Serialization;
using Application.Services.Abstractions;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Domain;
using Domain.Entities;
using Domain.Enums;
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

        if (typeof(T) == typeof(Transaction))
        {
            var random = new Random();
            var categories = Enum.GetValues<MerchantCategory>();
            foreach (var record in records.Cast<Transaction>())
            {
                // Если в CSV нет категории, рандомим её
                if (record.Category == default)
                    record.Category = categories[random.Next(categories.Length)];

                // Рандомим, является ли это партнером (например, 20% шанс)
                record.IsPartner = random.Next(100) < 20;
            }
        }

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
