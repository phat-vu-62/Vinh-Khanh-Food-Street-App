using System.Text.Json;
using FoodStreetApp.CMS.Models;
using System.Net.Http.Json;

namespace FoodStreetApp.CMS.Services;

public interface IAutoTranslationService
{
    Task<AutoTranslationResponse> TranslateAsync(AutoTranslationRequest request);
}

public class AutoTranslationService : IAutoTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AutoTranslationService> _logger;

    public AutoTranslationService(HttpClient httpClient, IConfiguration configuration, ILogger<AutoTranslationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AutoTranslationResponse> TranslateAsync(AutoTranslationRequest request)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                     ?? _configuration["Gemini:ApiKey"];

        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured.");
            throw new Exception("Gemini API key is missing.");
        }

        var systemPrompt = $@"
You are a professional translator. Please translate the following information into the target language of '{request.TargetLanguage}'.
Ensure the translation sounds natural and suits a public context (e.g. food and tourism application).

Original Shop Name: {request.OriginalShopName}
Original Description: {request.OriginalDescription}
Original TTS Script: {request.OriginalTtsScript}

Return ONLY a valid JSON object matching the exact structure below. Do NOT wrap it in markdown block like ```json.
{{
  ""translatedShopName"": ""..."",
  ""translatedDescription"": ""..."",
  ""translatedTtsScript"": ""...""
}}";

        var requestBody = new GeminiRequest
        {
            Contents = new List<GeminiContent>
            {
                new GeminiContent
                {
                    Parts = new List<GeminiPart>
                    {
                        new GeminiPart { Text = systemPrompt }
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
                throw new Exception($"Failed to contact Gemini API. Status code: {response.StatusCode}");
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            var jsonText = apiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(jsonText))
            {
                throw new Exception("Received empty response from Gemini API.");
            }

            jsonText = jsonText.Trim();
            // Clean markdown block just in case
            if (jsonText.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                var lines = jsonText.Split('\n');
                if (lines.Length >= 3)
                {
                    jsonText = string.Join("\n", lines.Skip(1).Reverse().Skip(1).Reverse());
                }
            }
            else if (jsonText.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                 var lines = jsonText.Split('\n');
                 if (lines.Length >= 3)
                 {
                     jsonText = string.Join("\n", lines.Skip(1).Reverse().Skip(1).Reverse());
                 }
            }

            var translationParams = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<AutoTranslationResponse>(jsonText, translationParams);

            return result ?? new AutoTranslationResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown during Auto Translate.");
            throw;
        }
    }
}
