using FoodStreetApp.Api.Interfaces;
using FoodStreetApp.Shared.Context;
using FoodStreetApp.Shared.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodStreetApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly CmsDbContext _context;

    public SyncController(CmsDbContext context)
    {
        _context = context;
    }

    [HttpGet("pois")]
    public async Task<ActionResult<IEnumerable<RemotePoiDto>>> GetPois()
    {
        // For the mobile app, we only sync approved and active POIs
        var pois = await _context.Pois
            // .Where(p => p.IsApproved && p.IsActive) // Optional: restrict to approved/active
            .ToListAsync();

        return Ok(pois.Select(MapToDto));
    }

    [HttpGet("poi-actions")]
    public async Task<ActionResult<IEnumerable<RemotePoiActionDto>>> GetPoiActions([FromQuery] string sinceUtc)
    {
        if (!DateTime.TryParse(sinceUtc, out var since))
        {
            return BadRequest("Invalid date format. Use ISO 8601.");
        }

        var actions = await _context.PoiSyncActions
            .Include(a => a.Poi)
            .Where(a => a.OccurredAtUtc > since.ToUniversalTime())
            .ToListAsync();

        return Ok(actions.Select(a => new RemotePoiActionDto
        {
            PoiId = a.PoiId,
            Action = a.Action,
            OccurredAtUtc = a.OccurredAtUtc,
            Poi = a.Poi != null ? MapToDto(a.Poi) : null
        }));
    }

    private static RemotePoiDto MapToDto(POI p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Latitude = p.Latitude,
        Longitude = p.Longitude,
        Radius = p.RadiusMeters,
        ApproachRadius = 200, // Default for mobile app
        Priority = p.Priority,
        Rating = 4.5, // Mocked for now
        ReviewCount = 0,
        Description = p.Description,
        AudioFile = p.AudioUrl,
        TtsText = p.TextContent,
        TtsTextEn = p.TextContentEn,
        TtsTextKo = p.TextContentKo,
        TtsTextZh = p.TextContentZh,
        TtsTextJa = p.TextContentJa,
        UseTts = !string.IsNullOrEmpty(p.TextContent),
        CooldownSeconds = 60,
        IsActive = p.IsActive && p.IsApproved,
        ImageUrl = p.ImageUrl
    };

    public class RemotePoiDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Radius { get; set; }
        public double ApproachRadius { get; set; }
        public int Priority { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public string? Description { get; set; }
        public string? AudioFile { get; set; }
        public string? TtsText { get; set; }
        public string? TtsTextEn { get; set; }
        public string? TtsTextKo { get; set; }
        public string? TtsTextZh { get; set; }
        public string? TtsTextJa { get; set; }
        public bool UseTts { get; set; }
        public int CooldownSeconds { get; set; }
        public bool IsActive { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class RemotePoiActionDto
    {
        public int PoiId { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
        public RemotePoiDto? Poi { get; set; }
    }
}
