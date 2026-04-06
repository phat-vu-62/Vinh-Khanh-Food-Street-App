using System.Net.Http.Json;
using System.Text.Json;
using FoodStreetApp.CMS.Models;
using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.CMS.Services;

public interface IGeminiTranslationService
{
    Task TranslatePoiAsync(POI poi);
}

public class GeminiTranslationService : IGeminiTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiTranslationService> _logger;
    private readonly ToastService _toastService;

    public GeminiTranslationService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<GeminiTranslationService> logger,
        ToastService toastService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _toastService = toastService;
    }

    public async Task TranslatePoiAsync(POI poi)
    {
        try
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                         ?? _configuration["Gemini:ApiKey"];

            var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Gemini API key is not configured.");
                return;
            }

            if (string.IsNullOrWhiteSpace(poi.Name) && string.IsNullOrWhiteSpace(poi.Description))
            {
                return;
            }

            var prompt = $@"
            You are a professional translator for a food and tourism application. 
            Please translate the following Name, Description, and TTS Text Content into Vietnamese (vi), English (en), Chinese (zh), Korean (ko), and Japanese (ja). 
            Make sure the translation is natural and context-aware (not word-by-word).
            
            Original Name: {poi.Name}
            Original Description: {poi.Description}
            Original TTS Text Content: {poi.TextContent}
            
            Return ONLY a JSON object matching the following structure exactly (do not wrap in markdown or anything else):
            {{
                ""nameVi"": ""..."",
                ""nameEn"": ""..."",
                ""nameZh"": ""..."",
                ""nameKo"": ""..."",
                ""nameJa"": ""..."",
                ""descriptionVi"": ""..."",
                ""descriptionEn"": ""..."",
                ""descriptionZh"": ""..."",
                ""descriptionKo"": ""..."",
                ""descriptionJa"": ""..."",
                ""textContentVi"": ""..."",
                ""textContentEn"": ""..."",
                ""textContentZh"": ""..."",
                ""textContentKo"": ""..."",
                ""textContentJa"": ""...""
            }}
            ";

            var requestBody = new GeminiRequest
            {
                Contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = prompt }
                        }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    ResponseMimeType = "application/json"
                }
            };

            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                _toastService.Error("Dịch tự động tạm gián đoạn. Vui lòng nhập liệu thủ công.");
                return; // Gracefully return, preserving existing values
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            var jsonText = apiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                jsonText = jsonText.Trim();
                if (jsonText.StartsWith("```json") || jsonText.StartsWith("```"))
                {
                    var lines = jsonText.Split('\n');
                    jsonText = string.Join("\n", lines.Skip(1).Reverse().Skip(1).Reverse());
                }

                var translationParams = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var translation = JsonSerializer.Deserialize<TranslationResult>(jsonText, translationParams);

                if (translation != null)
                {
                    poi.NameVi = translation.NameVi ?? poi.Name;
                    poi.NameEn = translation.NameEn ?? poi.NameEn;
                    poi.NameZh = translation.NameZh ?? poi.NameZh;
                    poi.NameKo = translation.NameKo ?? poi.NameKo;
                    poi.NameJa = translation.NameJa ?? poi.NameJa;

                    poi.DescriptionVi = translation.DescriptionVi ?? poi.DescriptionVi;
                    poi.DescriptionEn = translation.DescriptionEn ?? poi.DescriptionEn;
                    poi.DescriptionZh = translation.DescriptionZh ?? poi.DescriptionZh;
                    poi.DescriptionKo = translation.DescriptionKo ?? poi.DescriptionKo;
                    poi.DescriptionJa = translation.DescriptionJa ?? poi.DescriptionJa;

                    poi.TextContentVi = translation.TextContentVi ?? poi.TextContentVi;
                    poi.TextContentEn = translation.TextContentEn ?? poi.TextContentEn;
                    poi.TextContentZh = translation.TextContentZh ?? poi.TextContentZh;
                    poi.TextContentKo = translation.TextContentKo ?? poi.TextContentKo;
                    poi.TextContentJa = translation.TextContentJa ?? poi.TextContentJa;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini translation service exception occurred.");
            _toastService.Info("Kết nối API tự động tạm gián đoạn. Vui lòng kiểm tra lại thủ công.");
            // On catastrophic failure, the POI values are simply not updated (remaining as they were)
        }
    }
}
