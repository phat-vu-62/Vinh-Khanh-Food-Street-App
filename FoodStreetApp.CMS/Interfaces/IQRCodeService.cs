using FoodStreetApp.Shared.DTOs;

namespace FoodStreetApp.CMS.Interfaces;

public interface IQRCodeService
{
    string GenerateQRCodeBase64(string text);
    Task<IEnumerable<RestaurantQRCodeDto>> GetRestaurantQRCodesAsync(string baseUri);
}
