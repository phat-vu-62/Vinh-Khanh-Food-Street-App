namespace FoodStreetApp.Shared.DTOs;

public class AudioDto
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public bool IsActive { get; set; }
}
