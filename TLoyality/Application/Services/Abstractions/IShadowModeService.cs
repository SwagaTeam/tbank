using Application.Models;

namespace Application.Services.Abstractions;

public interface IShadowModeService
{
    Task<ShadowRecommendationResponse?> GetShadowRecommendation(int userId);
    Task<string?> GetChatResponse(int userId, string userMessage);
    public Task<string?> GetQuickSavingsHighlightAsync(int userId);
}