using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.Api.Interfaces;

public interface INarrationEngine
{
    string BuildNarration(POI poi);
}
