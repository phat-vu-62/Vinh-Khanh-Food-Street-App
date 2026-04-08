namespace FoodStreetApp.Shared.DTOs;

public class RestaurantQRCodeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public string DynamicUrl { get; set; } = string.Empty;
    public string QRCodeBase64 { get; set; } = string.Empty;
}
