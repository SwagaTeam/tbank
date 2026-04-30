using System.Globalization;
using System.Runtime.Serialization;
using Application.Services.Abstractions;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Abstractions; 
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.Implementations;

/// <summary>
/// Сервис для импорта данных из CSV-файлов с поддержкой автоматической генерации связанных транзакций.
/// </summary>
/// <param name="serviceProvider">Провайдер сервисов для разрешения зависимостей репозиториев во время выполнения.</param>
public class CsvImportService(IServiceProvider serviceProvider) : ICsvImportService
{
    /// <summary>
    /// Выполняет асинхронный импорт объектов типа <typeparamref name="T"/> из предоставленного CSV-файла.
    /// </summary>
    /// <typeparam name="T">Тип сущности, в который будут десериализованы строки CSV.</typeparam>
    /// <param name="file">Файл формата CSV, полученный из HTTP-запроса.</param>
    /// <returns>Количество успешно импортированных записей.</returns>
    public async Task<int> ImportAsync<T>(IFormFile? file) where T : class
    {
        if (file == null || file.Length == 0) return 0;

        var repository = serviceProvider.GetRequiredService<IRepository<T>>();

        using var reader = new StreamReader(file.OpenReadStream());
        
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            // Упрощаем сопоставление заголовков: удаляем подчеркивания и приводим к нижнему регистру,
            // чтобы свойства в C# (PascalCase) мапились на csv_headers (snake_case) без явных атрибутов.
            PrepareHeaderForMatch = args => args.Header.Replace("_", "").ToLower(),
            HeaderValidated = null, 
            MissingFieldFound = null
        };

        using var csv = new CsvReader(reader, config);
    
        // Регистрируем кастомные конвертеры для корректной обработки EnumMemberAttribute в перечислениях.
        csv.Context.TypeConverterCache.AddConverter<LoyaltyProgramName>(new EnumMemberConverter<LoyaltyProgramName>());
        csv.Context.TypeConverterCache.AddConverter<CashbackCurrency>(new EnumMemberConverter<CashbackCurrency>());
        
        var records = csv.GetRecords<T>().ToList();

        if (records.Count <= 0) return records.Count;

        // Паттерн-матчинг типа: если импортируется история лояльности, 
        // запускаем триггер на создание имитационных транзакций для наполнения БД.
        if (typeof(T) == typeof(LoyaltyHistory))
        {
            await GenerateTransactionsFromHistory(records.Cast<LoyaltyHistory>());
        }

        await repository.AddRangeAsync(records);
        await repository.SaveChangesAsync();

        return records.Count;
    }

    /// <summary>
    /// Генерирует случайный набор транзакций на основе записей о выплате кешбэка.
    /// Используется для симуляции финансовой активности пользователя.
    /// </summary>
    /// <param name="historyRecords">Коллекция записей истории лояльности.</param>
    private async Task GenerateTransactionsFromHistory(IEnumerable<LoyaltyHistory> historyRecords)
    {
        var transactionRepo = serviceProvider.GetRequiredService<ITransactionRepository>();
        var random = new Random();
        var categories = Enum.GetValues<MerchantCategory>();
        var simulatedTransactions = new List<Transaction>();

        foreach (var history in historyRecords)
        {
            var transactionsCount = random.Next(2, 6);

            for (var i = 0; i < transactionsCount; i++)
            {
                // Логика обратного расчета: мы знаем сумму выплаченного кешбэка и имитируем, 
                // что он составил 1-5% от суммы покупки. Множитель от 20 до 100 как раз 
                // восстанавливает исходную сумму покупки (1/0.05=20, 1/0.01=100).
                var partOfCashback = (decimal)history.CashbackAmount / transactionsCount;
                var multiplier = random.Next(20, 101);

                var transaction = new Transaction
                {
                    AccountId = history.AccountId,
                    Amount = Math.Round(partOfCashback * multiplier, 2),
                    TransactionDate = history.PayoutDate.AddDays(-random.Next(1, 8)),
                    Category = categories[random.Next(categories.Length)],
                    IsPartner = random.Next(100) < 15 
                };

                simulatedTransactions.Add(transaction);
            }
        }

        await transactionRepo.AddRangeAsync(simulatedTransactions);
        await transactionRepo.SaveChangesAsync();
    }
}

/// <summary>
/// Конвертер для перечислений, который учитывает атрибут [EnumMember] при парсинге строк.
/// </summary>
/// <typeparam name="T">Тип перечисления.</typeparam>
public class EnumMemberConverter<T>() : EnumConverter(typeof(T))
    where T : struct, Enum
{
    /// <summary>
    /// Преобразует строковое значение из CSV в соответствующий элемент перечисления.
    /// </summary>
    /// <param name="text">Строка из CSV.</param>
    /// <param name="row">Текущая строка данных.</param>
    /// <param name="memberMapData">Данные о маппинге свойства.</param>
    /// <returns>Значение перечисления или значение по умолчанию.</returns>
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return default(T);

        // Перебор полей Enum через рефлексию для поиска соответствия атрибуту [EnumMember(Value = "...")].
        // Это необходимо, так как стандартный EnumConverter ищет только по именам констант.
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