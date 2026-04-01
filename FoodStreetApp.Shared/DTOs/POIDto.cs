using FoodStreetApp.Shared.Enums;

namespace FoodStreetApp.Shared.DTOs;

public class POIDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; }
    public bool IsActive { get; set; }
    public POIType Type { get; set; }
}
