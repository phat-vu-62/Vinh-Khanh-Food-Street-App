using FoodStreetApp.Shared.Enums;

namespace FoodStreetApp.CMS.ViewModels;

public class PoiFormViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? AudioUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int RadiusMeters { get; set; } = 15;
    public POIType Type { get; set; } = POIType.Food;
}
