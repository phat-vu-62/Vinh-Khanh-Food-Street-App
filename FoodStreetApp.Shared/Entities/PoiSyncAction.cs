namespace FoodStreetApp.Shared.Entities;

public class PoiSyncAction
{
    public long Id { get; set; }
    public int PoiId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    
    public POI? Poi { get; set; }

}
