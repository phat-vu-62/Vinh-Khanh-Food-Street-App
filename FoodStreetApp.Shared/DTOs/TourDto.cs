namespace FoodStreetApp.Shared.DTOs;

public class TourDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<int> PoiIds { get; set; } = new();
}
