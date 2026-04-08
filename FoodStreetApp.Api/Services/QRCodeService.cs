using FoodStreetApp.Api.Interfaces;
using FoodStreetApp.Shared.DTOs;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace FoodStreetApp.Api.Services;

public class QRCodeService : IQRCodeService
{
    private readonly IPOIService _poiService;

    public QRCodeService(IPOIService poiService)
    {
        _poiService = poiService;
    }

    public string GenerateQRCodeBase64(string text)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);
        return Convert.ToBase64String(qrCodeAsPngByteArr);
    }

    public async Task<IEnumerable<RestaurantQRCodeDto>> GetRestaurantQRCodesAsync(string baseUri)
    {
        var pois = await _poiService.GetAllAsync();
        var dtos = new List<RestaurantQRCodeDto>();

        foreach (var poi in pois)
        {
            // dynamicUrl example: https://mydomain.com/listen/123
            string dynamicUrl = $"{baseUri.TrimEnd('/')}/listen/{poi.Id}";
            
            dtos.Add(new RestaurantQRCodeDto
            {
                Id = poi.Id,
                Name = poi.Name,
                AudioUrl = poi.AudioUrl ?? string.Empty,
                DynamicUrl = dynamicUrl,
                QRCodeBase64 = GenerateQRCodeBase64(dynamicUrl)
            });
        }

        return dtos;
    }
}
