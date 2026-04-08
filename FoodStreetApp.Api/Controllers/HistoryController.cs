using FoodStreetApp.Shared.Context;
using FoodStreetApp.Shared.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FoodStreetApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoryController : ControllerBase
{
    private readonly CmsDbContext _dbContext;
    private readonly ILogger<HistoryController> _logger;

    public HistoryController(CmsDbContext dbContext, ILogger<HistoryController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> TrackEvent([FromBody] UserHistory history)
    {
        try
        {
            // 1. HARD VALIDATION
            if (history.PoiId <= 0)
            {
                _logger.LogWarning("[Tracking] REFUSED: Invalid PoiId {PoiId}", history.PoiId);
                return BadRequest("Invalid PoiId");
            }

            if (string.IsNullOrEmpty(history.Action))
            {
                _logger.LogWarning("[Tracking] REFUSED: Missing Action for POI {PoiId}", history.PoiId);
                return BadRequest("Action required");
            }

            // 2. PRODUCTION LOGGING (Crucial for debugging mapping issues)
            _logger.LogInformation(
                "[Tracking] POI={PoiId} | Action={Action} | QR={QR}",
                history.PoiId,
                history.Action,
                history.QRCode ?? "None"
            );

            // 3. BACKEND AS SOURCE OF TRUTH (TIMESTAMPS)
            history.VisitedAtUtc = DateTime.UtcNow.AddHours(7);

            // 4. PERSIST
            _dbContext.UserHistories.Add(history);
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Tracking] CRITICAL ERROR: Database persistence failed");
            return StatusCode(500, "Internal server error");
        }
    }
}
