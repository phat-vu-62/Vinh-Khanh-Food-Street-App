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

    public GeminiTranslationService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiTranslationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task TranslatePoiAsync(POI poi)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                     ?? _configuration["Gemini:ApiKey"];

        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured. Skipping translations.");
            return;
        }

        if (string.IsNullOrWhiteSpace(poi.Name) && string.IsNullOrWhiteSpace(poi.Description))
        {
            return;
        }

        var prompt = $@"
        You are a professional translator for a food and tourism application. 
        Please translate the following Name and Description into Vietnamese (vi), English (en), Chinese (zh), Korean (ko), and Japanese (ja). 
        Make sure the translation is natural and context-aware (not word-by-word).
        
        Original Name: {poi.Name}
        Original Description: {poi.Description}
        
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
            ""descriptionJa"": ""...""
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

        try
        {
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API translation failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new Exception($"Gemini API returned {response.StatusCode}");
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            var jsonText = apiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                jsonText = jsonText.Trim();
                // Clean up possible markdown code blocks if the AI ignored output constraints
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
                    poi.NameEn = translation.NameEn ?? string.Empty;
                    poi.NameZh = translation.NameZh ?? string.Empty;
                    poi.NameKo = translation.NameKo ?? string.Empty;
                    poi.NameJa = translation.NameJa ?? string.Empty;

                    poi.DescriptionVi = translation.DescriptionVi ?? poi.Description;
                    poi.DescriptionEn = translation.DescriptionEn;
                    poi.DescriptionZh = translation.DescriptionZh;
                    poi.DescriptionKo = translation.DescriptionKo;
                    poi.DescriptionJa = translation.DescriptionJa;

                    _logger.LogInformation("Successfully translated POI '{Name}' into 5 languages.", poi.Name);
                }
            }
        }
        catch (Exception ex) when (ex.Message.Contains("Gemini API returned"))
        {
            throw; // Re-throw Gemini API errors so the UI can show them
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown while calling Gemini API for translations.");
            throw new Exception("Translation service error. Please try again later.", ex);
        }
    }
}
