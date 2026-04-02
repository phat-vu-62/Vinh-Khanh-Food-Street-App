using System.Text.Json.Serialization;

namespace FoodStreetApp.Api.Models;

public class GeminiRequest
{
    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = new();

    [JsonPropertyName("generationConfig")]
    public GeminiGenerationConfig? GenerationConfig { get; set; }
}

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = new();
}

public class GeminiPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class GeminiGenerationConfig
{
    [JsonPropertyName("responseMimeType")]
    public string? ResponseMimeType { get; set; }
    
    // We can enforce JSON schema if needed, but responseMimeType="application/json" usually works fine 
    // when prompt specifies the JSON structure.
}

public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }
}

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }
}

public class TranslationResult
{
    [JsonPropertyName("nameVi")]
    public string? NameVi { get; set; }
    [JsonPropertyName("nameEn")]
    public string? NameEn { get; set; }
    [JsonPropertyName("nameZh")]
    public string? NameZh { get; set; }
    [JsonPropertyName("nameKo")]
    public string? NameKo { get; set; }
    [JsonPropertyName("nameJa")]
    public string? NameJa { get; set; }

    [JsonPropertyName("descriptionVi")]
    public string? DescriptionVi { get; set; }
    [JsonPropertyName("descriptionEn")]
    public string? DescriptionEn { get; set; }
    [JsonPropertyName("descriptionZh")]
    public string? DescriptionZh { get; set; }
    [JsonPropertyName("descriptionKo")]
    public string? DescriptionKo { get; set; }
    [JsonPropertyName("descriptionJa")]
    public string? DescriptionJa { get; set; }
}
