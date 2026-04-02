using System.Text.Json.Serialization;

namespace FoodStreetApp.CMS.Models;

public class AutoTranslationRequest
{
    public string OriginalShopName { get; set; } = string.Empty;
    public string OriginalDescription { get; set; } = string.Empty;
    public string OriginalTtsScript { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = "en"; // Example: "en", "ja", "ko", "zh"
}

public class AutoTranslationResponse
{
    [JsonPropertyName("translatedShopName")]
    public string TranslatedShopName { get; set; } = string.Empty;

    [JsonPropertyName("translatedDescription")]
    public string TranslatedDescription { get; set; } = string.Empty;

    [JsonPropertyName("translatedTtsScript")]
    public string TranslatedTtsScript { get; set; } = string.Empty;
}
