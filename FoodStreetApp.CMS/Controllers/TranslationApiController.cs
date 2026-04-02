using Microsoft.AspNetCore.Mvc;
using FoodStreetApp.CMS.Models;
using FoodStreetApp.CMS.Services;

namespace FoodStreetApp.CMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslationApiController : ControllerBase
{
    private readonly IAutoTranslationService _translationService;
    private readonly ILogger<TranslationApiController> _logger;

    public TranslationApiController(IAutoTranslationService translationService, ILogger<TranslationApiController> logger)
    {
        _translationService = translationService;
        _logger = logger;
    }

    [HttpPost("translate")]
    public async Task<IActionResult> Translate([FromBody] AutoTranslationRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.TargetLanguage))
        {
            return BadRequest(new { Message = "Invalid translation request." });
        }

        try
        {
            var response = await _translationService.TranslateAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error while translating.");
            return StatusCode(500, new { Message = "Lỗi khi gọi dịch vụ dịch thuật do máy chủ xử lý hoặc cấu hình." });
        }
    }
}
