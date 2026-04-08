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
        // 1. Log FULL request payload for production debugging
        var jsonPayload = JsonSerializer.Serialize(history);
        _logger.LogInformation("[Tracking] Received payload: {Payload}", jsonPayload);

        try
        {
            // 2. Harden Validation
            if (history.PoiId <= 0)
            {
                _logger.LogWarning("[Tracking] Validation failed: Invalid PoiId {PoiId}", history.PoiId);
                return BadRequest(new { error = "PoiId must be a positive integer" });
            }

            if (string.IsNullOrWhiteSpace(history.Action))
            {
                _logger.LogWarning("[Tracking] Validation failed: Missing Action for POI {PoiId}", history.PoiId);
                return BadRequest(new { error = "Action is required" });
            }

            // 3. Normalize data
            history.VisitedAtUtc = DateTime.UtcNow;
            if (history.QRCode != null)
            {
                _logger.LogInformation("[Tracking] Event triggered by QR: {QRCode}", history.QRCode);
            }

            // 4. Save with careful Await and verification
            _logger.LogDebug("[Tracking] Attempting to save record to database...");
            _dbContext.UserHistories.Add(history);
            
            var result = await _dbContext.SaveChangesAsync();

            if (result > 0)
            {
                _logger.LogInformation("[Tracking] SUCCESS: Saved record ID {Id} for Action {Action}", 
                    history.Id, history.Action);
                
                return Ok(new { success = true, id = history.Id });
            }
            else
            {
                _logger.LogError("[Tracking] FAILURE: SaveChangesAsync returned 0 records affected.");
                return StatusCode(500, new { error = "Data was not saved to database" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Tracking] CRITICAL ERROR saving history record for POI {PoiId}", history.PoiId);
            return StatusCode(500, new { error = "Internal server error during persistence", details = ex.Message });
        }
    }
}
