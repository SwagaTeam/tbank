using System.Text.Json.Serialization;

namespace Application.Models;

public record ShadowRecommendationResponse(
    [property: JsonPropertyName("current_status")] string CurrentStatus,
    [property: JsonPropertyName("lost_profit_highlight")] string LostProfitHighlight,
    [property: JsonPropertyName("recommendation_text")] string RecommendationText,
    [property: JsonPropertyName("target_program_id")] int TargetProgramId
)
{
    public static ShadowRecommendationResponse Default(string name) => new(
        $"Привет, {name}!",
        "Теневой режим готов к расчету",
        "Начните пользоваться картой, и я покажу, как выжать из банка максимум.",
        1
    );
}