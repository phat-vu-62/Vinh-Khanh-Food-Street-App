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
    private readonly IConfiguration _configuration;

    public HistoryController(CmsDbContext dbContext, ILogger<HistoryController> logger, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> TrackEvent([FromBody] UserHistory history)
    {
        _logger.LogInformation("====================================");
        _logger.LogInformation("[DEBUG-API] HIT: HistoryController.TrackEvent");
        
        var jsonPayload = JsonSerializer.Serialize(history);
        _logger.LogInformation("[DEBUG-API] PAYLOAD: {Payload}", jsonPayload);

        // Masked Connection string to verify correct DB
        var conn = _configuration.GetConnectionString("Postgres") ?? "NULL";
        var maskedConn = conn.Length > 20 ? conn.Substring(0, 20) + "..." : conn;
        _logger.LogInformation("[DEBUG-API] DATABASE: {Conn}", maskedConn);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[DEBUG-API] ⚠️ MODEL STATE INVALID: {Errors}", 
                string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        if (history.PoiId <= 0 || string.IsNullOrWhiteSpace(history.Action))
            {
                _logger.LogWarning("[DEBUG-API] ⚠️ INVALID DATA: PoiId={PoiId}, Action={Action}", history.PoiId, history.Action);
                return BadRequest(new { error = "Invalid PoiId or Action" });
            }

            // Get count before
            var countBefore = await _dbContext.UserHistories.CountAsync();
            _logger.LogInformation("[DEBUG-API] Record count BEFORE: {Count}", countBefore);

            history.VisitedAtUtc = DateTime.UtcNow;
            _dbContext.UserHistories.Add(history);
            
            _logger.LogInformation("[DEBUG-API] Saving changes...");
            var result = await _dbContext.SaveChangesAsync();

            // Get count after
            var countAfter = await _dbContext.UserHistories.CountAsync();
            _logger.LogInformation("[DEBUG-API] Record count AFTER: {Count}", countAfter);

            if (result > 0)
            {
                _logger.LogInformation("[DEBUG-API] ✅ SUCCESS: Record {Id} persisted", history.Id);
                return Ok(new { success = true, id = history.Id });
            }
            else
            {
                _logger.LogError("[DEBUG-API] ❌ FAILURE: SaveChangesAsync returned 0");
                return StatusCode(500, new { error = "Save failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEBUG-API] ❌ CRITICAL ERROR: {Message}", ex.Message);
            return StatusCode(500, new { error = "Server error", message = ex.Message });
        }
        finally
        {
            _logger.LogInformation("====================================");
        }
    }
}
