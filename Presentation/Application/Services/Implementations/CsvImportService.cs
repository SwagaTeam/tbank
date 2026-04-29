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

        if (typeof(T) == typeof(LoyaltyHistory))
        {
            await GenerateTransactionsFromHistory(records.Cast<LoyaltyHistory>());
        }

        await repository.AddRangeAsync(records);
        await repository.SaveChangesAsync();

        return records.Count;
    }

    private async Task GenerateTransactionsFromHistory(IEnumerable<LoyaltyHistory> historyRecords)
    {
        var transactionRepo = serviceProvider.GetRequiredService<ITransactionRepository>();
        var random = new Random();
        var categories = Enum.GetValues<MerchantCategory>();
        var simulatedTransactions = new List<Transaction>();

        foreach (var history in historyRecords)
        {
            // На одну выплату кешбэка генерируем 2-5 транзакций
            var transactionsCount = random.Next(2, 6);

            for (int i = 0; i < transactionsCount; i++)
            {
                // Имитируем, что средний кешбэк — это 1-5% от покупки
                // Поэтому сумма транзакции = (Кешбэк / Кол-во транзакций) * Рандомный множитель
                var partOfCashback = (decimal)history.CashbackAmount / transactionsCount;
                var multiplier = random.Next(20, 101); // множитель от x20 до x100

                var transaction = new Transaction
                {
                    AccountId = history.AccountId,
                    Amount = Math.Round(partOfCashback * multiplier, 2),
                    // Дата транзакции — за 1-7 дней до выплаты кешбэка
                    TransactionDate = history.PayoutDate.AddDays(-random.Next(1, 8)),
                    Category = categories[random.Next(categories.Length)],
                    IsPartner = random.Next(100) < 15 // 15% шанс, что это партнер
                };

                simulatedTransactions.Add(transaction);
            }
        }

        await transactionRepo.AddRangeAsync(simulatedTransactions);
        await transactionRepo.SaveChangesAsync();
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
