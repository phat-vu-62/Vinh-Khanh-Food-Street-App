using FoodStreetApp.Api.Interfaces;
using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.Api.Services;

public class NarrationEngine : INarrationEngine
{
    public string BuildNarration(POI poi)
    {
        return string.IsNullOrWhiteSpace(poi.Description)
            ? $"Bạn đang đến {poi.Name}."
            : $"Bạn đang đến {poi.Name}. {poi.Description}";
    }
}
