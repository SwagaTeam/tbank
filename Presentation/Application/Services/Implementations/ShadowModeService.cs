using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using Application.Models;

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

        var systemMessage =
            @"Ты — лаконичное аналитическое ядро «T-Shadow Mode». Твоя задача: конвертировать транзакции в «упущенную выгоду».

            ### КРИТИЧЕСКИЕ ОГРАНИЧЕНИЯ (СТРОГО):
            1. ОБЩИЙ ОБЪЕМ: Весь ответ не должен превышать 10 предложений.
            2. ПОЛЯ JSON:
               - current_status: 1-2 предложения.
               - lost_profit_highlight: короткая фраза до 10 слов.
               - recommendation_text: максимум 3-5 предложений.
            3. ФОРМАТ: Только чистый JSON. Никаких приветствий и пояснений.

            ### ПРАВИЛА АНАЛИЗА:
            1. ОПТИМИЗАЦИЯ: Сравнивай текущий кэшбэк с потенциалом в All Airlines (мили) или Platinum (Bravo).
            2. ЭКОСИСТЕМА: Связь > 1000р -> Т-Мобайл; Баланс > 50к -> Инвестиции; Супермаркеты -> Platinum; Трэвел > 20% -> All Airlines.
            3. ТОН: Экспертный, лаконичный, с легкой иронией. Используй только дефисы (-), запрещены длинные тире (—).

            ### СТРУКТУРА JSON:
            {
              ""current_status"": ""Факт начислений сейчас (1-2 предложения)"",
              ""lost_profit_highlight"": ""Бьющая в цель фраза про упущенную выгоду"",
              ""recommendation_text"": ""Анализ с цифрами и логикой: факт, боль, действие (до 5 предложений)"",
              ""target_program_id"": 1
            }
            НЕ ПИШИ БОЛЬШЕ 5 ПРЕДЛОЖЕний"
            ;

        var userPrompt = $@"
            ПОЛЬЗОВАТЕЛЬ: {context.User.FullName} (Сегмент: {context.User.FinancialSegment})
            АККАУНТЫ: {string.Join(", ", context.CurrentAccount.Select(a => $"ID {a.LoyaltyProgramId} (Balance: {a.CurrentBalance})"))}
            ИСТОРИЯ КЭШБЭКА: {string.Join("; ", cashbackAnalysis)}
            ДОСТУПНО ДЛЯ ПЕРЕХОДА: {string.Join(", ", context.AvailablePrograms.Select(p => p.LoyaltyProgramName))}
            ОФФЕРЫ: {string.Join(", ", context.RelevantOffers.Take(5).Select(o => o.PartnerName))}";

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
            max_tokens  = 500,
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
        
        var content = result.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrEmpty(content))
            throw new Exception("OpenRouter returned success, but message content is empty.");

        try 
        {
            Console.WriteLine(content);
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
    
        var cashbackAnalysis = context.RecentHistory
            .GroupBy(h => h.AccountId)
            .Select(g => $"Аккаунт {g.Key}: {g.Sum(x => x.CashbackAmount)} кэшбэка.");

        var programsInfo = string.Join("; ", context.AvailablePrograms
            .Select(p => $"{p.LoyaltyProgramName} (ID: {p.LoyaltyProgramId})"));

        var systemMessage = $@"Ты — финансовый ассистент T-Shadow. 
            Ты эксперт по программам: {programsInfo}.

            ДАННЫЕ ПОЛЬЗОВАТЕЛЯ:
            - Имя: {context.User.FullName}
            - Аккаунты: {string.Join(", ", context.CurrentAccount.Select(a => $"ID {a.LoyaltyProgramId} (Баланс: {a.CurrentBalance})"))}
            - Накопленный кэшбэк: {string.Join("; ", cashbackAnalysis)}

            ### КРИТИЧЕСКИЕ ПРАВИЛА:
            1. НИКАКОЙ НЕОПРЕДЕЛЕННОСТИ: Запрещено говорить ""сравните условия"" или ""выберите сами"". Ты ДОЛЖЕН провести расчет и сказать, какой ID программы или продукт выгоднее прямо сейчас.
            2. КОНКРЕТИКА: Используй только данные выше. Если покупка в категории 'Путешествия', а у пользователя есть All Airlines (ID 1), говори: 'Используй карту All Airlines, это даст 10% милями вместо 1%'.
            3. ЛИМИТ: 3-5 предложений.
            4. ЗАПРЕТ КАЗИНО: На любые вопросы об азартных играх отвечай одной короткой фразой о рисках и переходи к анализу обычных трат.
            5. СТИЛЬ: Без длинных тире (—), только дефисы (-).";

        var requestBody = new
        {
            model = "google/gemini-2.0-flash-001",
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = userMessage } 
            },
            temperature = 0.4,
            max_tokens = 500
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, Url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(requestBody);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return "Извини, я призадумался. Попробуй позже.";

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return result.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()?
            .Replace("\r\n", "") 
            .Replace("\n", "")  
            .Trim();
    }
}