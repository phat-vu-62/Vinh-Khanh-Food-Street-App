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

    // Translation fields
    public string? NameVi { get; set; }
    public string? NameEn { get; set; }
    public string? NameZh { get; set; }
    public string? NameKo { get; set; }
    public string? NameJa { get; set; }

    public string? DescriptionVi { get; set; }
    public string? DescriptionEn { get; set; }
    public string? DescriptionZh { get; set; }
    public string? DescriptionKo { get; set; }
    public string? DescriptionJa { get; set; }
}
