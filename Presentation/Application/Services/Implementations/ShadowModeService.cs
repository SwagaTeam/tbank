using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using Application.Models;
using Domain.Enums;

namespace Application.Services.Implementations;

internal class ShadowModeService(HttpClient httpClient, ILoyaltyService loyaltyService, IConfiguration configuration)
    : IShadowModeService
{
    private readonly string? _apiKey = Environment.GetEnvironmentVariable("apiKey");
    private const string Url = "https://openrouter.ai/api/v1/chat/completions";
    private const string Model = "google/gemini-2.0-flash-001";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<ShadowRecommendationResponse?> GetShadowRecommendation(int userId)
    {
        var context = await loyaltyService.GetShadowContext(userId);

        if (context.CurrentAccount.Count == 0)
        {
            return ShadowRecommendationResponse.Default(context.User.FullName);
        }

        var cashbackAnalysis = context.RecentHistory
            .GroupBy(h => h.AccountId)
            .Select(g =>
            {
                var acc = context.CurrentAccount.FirstOrDefault(a => a.AccountId == g.Key);
                var programName = context.AvailablePrograms
                    .FirstOrDefault(p => p.LoyaltyProgramId == acc?.LoyaltyProgramId)?.LoyaltyProgramName;
                return $"Программа {programName} (ID {acc?.LoyaltyProgramId}): {g.Sum(x => x.CashbackAmount)}";
            });

        var transactionStats = context.Transactions
            .GroupBy(t => t.Category)
            .Select(g => $"{g.Key}: {g.Sum(t => t.Amount)} руб. ({g.Count()} транз.)");

        var systemMessage =
            @"Ты — лаконичное аналитическое ядро «T-Shadow Mode». Твоя задача: конвертировать историю транзакций в «упущенную выгоду».

            ### КРИТИЧЕСКИЕ ОГРАНИЧЕНИЯ:
            1. ОБЪЕМ: Весь ответ до 5-7 предложений.
            2. ФОРМАТ: Только чистый JSON.
            3. ТОН: Экспертный, лаконичный, с легкой иронией. Без длинных тире (—), только дефисы (-).

            ### ЛОГИКА ОПТИМИЗАЦИИ:
            - Категория Supermarkets/Pharmacy -> карта Platinum (бонусы Bravo).
            - Категория GasStations/Restaurants -> если траты высокие, сравнивай с текущими начислениями.
            - Если траты по всем категориям > 50к -> предлагай премиальные программы.
            - Баланс > 50к без Инвестиций -> Т-Инвестиции.

            ### СТРУКТУРА JSON:
            {
              ""current_status"": ""Краткий факт начислений (1-2 предложения)"",
              ""lost_profit_highlight"": ""Фраза про упущенную выгоду до 10 слов"",
              ""recommendation_text"": ""Анализ транзакций: почему другая программа выгоднее (3-4 предложения)"",
              ""target_program_id"": 1
            }";

        var userPrompt = $@"
            ПОЛЬЗОВАТЕЛЬ: {context.User.FullName} (Сегмент: {context.User.FinancialSegment})
            АККАУНТЫ: {string.Join(", ", context.CurrentAccount.Select(a => $"ID {a.LoyaltyProgramId} (Balance: {a.CurrentBalance})"))}
            АНАЛИЗ ТРАТ: {string.Join("; ", transactionStats)}
            ИСТОРИЯ КЭШБЭКА: {string.Join("; ", cashbackAnalysis)}
            ДОСТУПНО ДЛЯ ПЕРЕХОДА: {string.Join(", ", context.AvailablePrograms.Select(p => $"{p.LoyaltyProgramName} (ID {p.LoyaltyProgramId})"))}
            ОФФЕРЫ: {string.Join(", ", context.RelevantOffers.Take(3).Select(o => o.PartnerName))}";

        var requestBody = new
        {
            model = Model,
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = userPrompt }
            },
            response_format = new { type = "json_object" },
            temperature = 0.2,
            max_tokens = 600,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, Url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Add("HTTP-Referer", "http://localhost:5000");
        request.Content = JsonContent.Create(requestBody);

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenRouter API Error: {response.StatusCode}. Details: {errorDetails}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var content = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        if (string.IsNullOrEmpty(content))
            throw new Exception("OpenRouter returned success, but message content is empty.");

        try
        {
            return JsonSerializer.Deserialize<ShadowRecommendationResponse>(content, _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new Exception($"Failed to parse AI JSON response. Raw content: {content}", ex);
        }
    }

    public async Task<string?> GetChatResponse(int userId, string userMessage)
    {
        var context = await loyaltyService.GetShadowContext(userId);

        var transactionStats = context.Transactions
            .GroupBy(t => t.Category)
            .Select(g => $"{g.Key.MapCategoryToRussian()}: {g.Sum(t => t.Amount)} руб.");

        var programsInfo = string.Join("; ", context.AvailablePrograms
            .Select(p => $"{p.LoyaltyProgramName} (ID: {p.LoyaltyProgramId})"));

        var systemMessage = $@"Ты — финансовый ассистент T-Shadow.
        Твоя база знаний: {programsInfo}.

        ДАННЫЕ ПОЛЬЗОВАТЕЛЯ:
        - Имя: {context.User.FullName}
        - Траты по категориям: {string.Join("; ", transactionStats)}
        - Аккаунты: {string.Join(", ", context.CurrentAccount.Select(a => $"ID {a.LoyaltyProgramId} (Баланс: {a.CurrentBalance})"))}

        ### ПРАВИЛА (СТРОГО):
        1. БУДЬ РЕШИТЕЛЬНЫМ: Анализируй траты. Если много трат в категории 'Супермаркеты', говори использовать Platinum. 
        2. ОДНА СТРОКА: Пиши без переносов строк.
        3. НИКАКИХ СЛЕШЕЙ: Не используй символы / или \ в тексте.
        4. ЛИМИТ: 3-5 предложений.
        5. СТИЛЬ: Только дефисы (-). Пиши фактами.";

        var requestBody = new
        {
            model = Model,
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = userMessage }
            },
            temperature = 0.3,
            max_tokens = 500
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, Url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(requestBody);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return "Извини, я призадумался. Попробуй позже.";

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var content = result.ValueKind != JsonValueKind.Undefined
            ? result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
            : null;

        return content?
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace("/", "")
            .Replace("\\", "")
            .Trim();
    }

    public async Task<string?> GetQuickSavingsHighlightAsync(int userId)
    {
        var context = await loyaltyService.GetShadowContext(userId);

        var lastMonthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));

        var lastMonthTransactions = context.Transactions
            .Where(t => t.TransactionDate >= lastMonthDate)
            .ToList();

        if (lastMonthTransactions.Count == 0)
        {
            return "В этом месяце вы можете начать экономить, совершив первую покупку.";
        }

        var topCategories = lastMonthTransactions
            .GroupBy(t => t.Category)
            .OrderByDescending(g => g.Sum(t => t.Amount))
            .Take(3)
            .Select(g => $"{g.Key.MapCategoryToRussian()}: {g.Sum(t => t.Amount)} руб.");

        var topOffers = context.RelevantOffers
            .Take(3)
            .Select(o => $"{o.PartnerName}");

        var systemMessage =
            @"Ты - финансовый аналитик. Твоя единственная цель - вычислить упущенную выгоду и выдать короткий тизер.
    
            ### КРИТИЧЕСКИЕ ПРАВИЛА:
            1. ФОРМАТ: Строго ОДНО предложение.
            2. ШАБЛОН: 'В этом месяце вы можете сэкономить еще [сумма] руб. на [категория или партнер].'
            3. СИМВОЛЫ: Никаких переносов строк. Строго запрещено использовать длинное тире, используй только обычный дефис (-).
            4. ЛОГИКА: Возьми самую крупную категорию трат, посчитай от нее примерно 5% (или используй подходящего партнера) и подставь в шаблон.";

        var userPrompt = $@"
            Траты пользователя за последний месяц: {string.Join("; ", topCategories)}
            Актуальные партнеры: {string.Join(", ", topOffers)}";

        var requestBody = new
        {
            model = Model,
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.3,
            max_tokens = 100
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, Url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(requestBody);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Безопасное извлечение контента
        string? content = null;
        if (result.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            content = choices[0].GetProperty("message").GetProperty("content").GetString();
        }

        return content?
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace("—", "-")
            .Trim();
    }
}