using FoodStreetApp.CMS.Interfaces;
using FoodStreetApp.Shared.DTOs;
using QRCoder;

namespace FoodStreetApp.CMS.Services;

public class QRCodeService : IQRCodeService
{
    private readonly IAdminDataService _adminDataService;

    public QRCodeService(IAdminDataService adminDataService)
    {
        _adminDataService = adminDataService;
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
        // For Blazor Server, we can use Task.FromResult or just run synchronously since GetPois is sync in this service
        var pois = _adminDataService.GetPois();
        var dtos = new List<RestaurantQRCodeDto>();

        foreach (var poi in pois)
        {
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

        return await Task.FromResult(dtos);
    }
}
