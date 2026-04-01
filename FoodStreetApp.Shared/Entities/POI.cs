using FoodStreetApp.Shared.Enums;

namespace FoodStreetApp.Shared.Entities;

public class POI
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AudioUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; } = 15;
    public bool IsActive { get; set; } = true;
    public POIType Type { get; set; } = POIType.Food;
}
