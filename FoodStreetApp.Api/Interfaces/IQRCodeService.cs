using FoodStreetApp.Shared.DTOs;

namespace FoodStreetApp.Api.Interfaces;

public interface IQRCodeService
{
    string GenerateQRCodeBase64(string text);
    Task<IEnumerable<RestaurantQRCodeDto>> GetRestaurantQRCodesAsync(string baseUri);
}
