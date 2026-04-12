using FoodStreetApp.Api.Interfaces;
using FoodStreetApp.Shared.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FoodStreetApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class POIController : ControllerBase
{
    private readonly IPOIService _service;
    private readonly IQRCodeService _qrCodeService;

    // Helper to get current user ID and role
    private int CurrentUserId => int.Parse(User.FindFirst("userId")?.Value ?? "0");
    private bool IsAdmin => User.IsInRole("admin");

    public POIController(IPOIService service, IQRCodeService qrCodeService)
    {
        _service = service;
        _qrCodeService = qrCodeService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<POI>>> GetAll()
    {
        var pois = await _service.GetAllAsync();
        if (IsAdmin) return Ok(pois);
        
        // If owner, only show their own POIs
        return Ok(pois.Where(p => p.OwnerId == CurrentUserId).ToList());
    }

    [HttpGet("qrcodes")]
    public async Task<ActionResult<IEnumerable<FoodStreetApp.Shared.DTOs.RestaurantQRCodeDto>>> GetQRCodes()
    {
        // In a real production app, baseUri should come from configuration
        string baseUri = $"{Request.Scheme}://{Request.Host}";
        var qrCodes = await _qrCodeService.GetRestaurantQRCodesAsync(baseUri);
        return Ok(qrCodes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<POI>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<POI>> Create([FromBody] POI poi)
    {
        // Force OwnerId to current user if not Admin
        if (!IsAdmin) poi.OwnerId = CurrentUserId;
        
        var created = await _service.CreateAsync(poi);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] POI poi)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing == null) return NotFound();
        
        // Security check: Owner can only update their own
        if (!IsAdmin && existing.OwnerId != CurrentUserId) return Forbid();

        var updated = await _service.UpdateAsync(id, poi);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing == null) return NotFound();
        
        // Security check: Owner can only delete their own
        if (!IsAdmin && existing.OwnerId != CurrentUserId) return Forbid();

        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
