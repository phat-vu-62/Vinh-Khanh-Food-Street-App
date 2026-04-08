using FoodStreetApp.Shared.Context;
using FoodStreetApp.Shared.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            _logger.LogInformation("[Tracking] Received request: {Action} for POI {PoiId} from User {UserId}", 
                history.Action, history.PoiId, history.UserId);

            if (history.PoiId <= 0)
            {
                _logger.LogWarning("[Tracking] Invalid POI ID: {PoiId}", history.PoiId);
                return BadRequest("Invalid POI ID");
            }

            // Ensure timestamp is UTC
            history.VisitedAtUtc = DateTime.UtcNow;

            _dbContext.UserHistories.Add(history);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("[Tracking] Successfully saved history record ID: {Id} for POI {PoiId}", 
                history.Id, history.PoiId);

            return Ok(new { success = true, id = history.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Tracking] Error saving history record for POI {PoiId}", history.PoiId);
            return StatusCode(500, "Internal server error");
        }
    }
}
