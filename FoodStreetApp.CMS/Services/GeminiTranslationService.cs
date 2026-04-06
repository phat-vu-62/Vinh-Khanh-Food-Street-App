using System.Net.Http.Json;
using System.Text.Json;
using FoodStreetApp.CMS.Models;
using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.CMS.Services;

public interface IGeminiTranslationService
{
    /// <summary>
    /// Translates a POI. Returns true if AI translation succeeded, false if fallback/error occurred.
    /// </summary>
    Task<bool> TranslatePoiAsync(POI poi);
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

    public async Task<bool> TranslatePoiAsync(POI poi)
    {
        try
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                         ?? _configuration["Gemini:ApiKey"];

            var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Gemini API key is not configured.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(poi.Name) && string.IsNullOrWhiteSpace(poi.Description))
            {
                return false;
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

            // Strategy: try v1beta first (supports responseMimeType), then v1 without it
            var endpoints = new[]
            {
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}",
                $"https://generativelanguage.googleapis.com/v1/models/{model}:generateContent?key={apiKey}",
            };

            HttpResponseMessage? response = null;

            foreach (var endpoint in endpoints)
            {
                _logger.LogInformation("Trying Gemini endpoint: {Endpoint}", endpoint.Split("?")[0]);
                response = await _httpClient.PostAsJsonAsync(endpoint, requestBody);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Gemini endpoint succeeded.");
                    break;
                }

                var errBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Gemini endpoint failed: {Status} - {Body}", response.StatusCode, errBody);

                // If BadRequest, the issue is likely responseMimeType — retry without it
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogInformation("Retrying without responseMimeType...");
                    var plainBody = new GeminiRequest
                    {
                        Contents = requestBody.Contents
                        // No GenerationConfig — let the model return free-form text
                    };
                    response = await _httpClient.PostAsJsonAsync(endpoint, plainBody);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Retry without responseMimeType succeeded.");
                        break;
                    }
                }
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                var statusCode = response?.StatusCode.ToString() ?? "Unknown";
                _toastService.Error($"Dịch thất bại (Lỗi {statusCode} - Model: {model}). Vui lòng nhập liệu thủ công.");
                return false;
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
                    poi.NameEn = translation.NameEn ?? (poi.NameEn ?? string.Empty);
                    poi.NameZh = translation.NameZh ?? (poi.NameZh ?? string.Empty);
                    poi.NameKo = translation.NameKo ?? (poi.NameKo ?? string.Empty);
                    poi.NameJa = translation.NameJa ?? (poi.NameJa ?? string.Empty);

                    poi.DescriptionVi = translation.DescriptionVi ?? (poi.DescriptionVi ?? string.Empty);
                    poi.DescriptionEn = translation.DescriptionEn ?? (poi.DescriptionEn ?? string.Empty);
                    poi.DescriptionZh = translation.DescriptionZh ?? (poi.DescriptionZh ?? string.Empty);
                    poi.DescriptionKo = translation.DescriptionKo ?? (poi.DescriptionKo ?? string.Empty);
                    poi.DescriptionJa = translation.DescriptionJa ?? (poi.DescriptionJa ?? string.Empty);

                    poi.TextContentVi = translation.TextContentVi ?? (poi.TextContentVi ?? string.Empty);
                    poi.TextContentEn = translation.TextContentEn ?? (poi.TextContentEn ?? string.Empty);
                    poi.TextContentZh = translation.TextContentZh ?? (poi.TextContentZh ?? string.Empty);
                    poi.TextContentKo = translation.TextContentKo ?? (poi.TextContentKo ?? string.Empty);
                    poi.TextContentJa = translation.TextContentJa ?? (poi.TextContentJa ?? string.Empty);
                    
                    return true;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini translation service exception occurred.");
            _toastService.Info("Kết nối API tự động tạm gián đoạn. Vui lòng kiểm tra lại thủ công.");
            return false;
        }
    }
}
