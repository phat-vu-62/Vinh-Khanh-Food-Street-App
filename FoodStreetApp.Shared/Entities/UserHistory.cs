namespace FoodStreetApp.Shared.Entities;

public class UserHistory
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int PoiId { get; set; }
    public DateTime VisitedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Action { get; set; }
}
