using FoodStreetApp.Api.Interfaces;
using FoodStreetApp.Shared.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FoodStreetApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class POIController : ControllerBase
{
    private readonly IPOIService _service;
    private readonly IQRCodeService _qrCodeService;

    public POIController(IPOIService service, IQRCodeService qrCodeService)
    {
        _service = service;
        _qrCodeService = qrCodeService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<POI>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
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
        var created = await _service.CreateAsync(poi);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] POI poi)
    {
        var updated = await _service.UpdateAsync(id, poi);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
