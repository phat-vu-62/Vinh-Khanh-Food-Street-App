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
        // 1. Log FULL request payload for production auditing
        var jsonPayload = JsonSerializer.Serialize(history);
        _logger.LogInformation("[Tracking] Received payload: {Payload}", jsonPayload);

        try
        {
            // 2. Strict Production Validation
            if (history.PoiId <= 0)
            {
                _logger.LogWarning("[Tracking] REFUSED: Invalid PoiId {PoiId}", history.PoiId);
                return BadRequest(new { error = "PoiId must be a positive integer" });
            }

            if (string.IsNullOrWhiteSpace(history.Action))
            {
                _logger.LogWarning("[Tracking] REFUSED: Missing Action for POI {PoiId}", history.PoiId);
                return BadRequest(new { error = "Action is required" });
            }

            // 3. Metadata Normalization
            history.VisitedAtUtc = DateTime.UtcNow;
            
            _logger.LogDebug("[Tracking] Persistence: Preparing to save to PostgreSQL...");

            // 4. Guaranteed Database Write
            _dbContext.UserHistories.Add(history);
            
            _logger.LogInformation("[Tracking] DB: Calling SaveChangesAsync...");
            var result = await _dbContext.SaveChangesAsync();

            if (result > 0)
            {
                _logger.LogInformation("[Tracking] SUCCESS: Record {Id} persisted in DB", history.Id);
                return Ok(new { success = true, id = history.Id });
            }
            else
            {
                _logger.LogError("[Tracking] FAILURE: No records were written to the database.");
                return StatusCode(500, new { error = "Database write failed without error message" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Tracking] CRITICAL ERROR: Database persistence failed for POI {PoiId}", history.PoiId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}
