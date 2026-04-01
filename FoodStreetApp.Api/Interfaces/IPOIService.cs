using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.Api.Interfaces;

public interface IPOIService
{
    Task<IReadOnlyCollection<POI>> GetAllAsync();
    Task<POI?> GetByIdAsync(int id);
    Task<POI> CreateAsync(POI poi);
    Task<bool> UpdateAsync(int id, POI poi);
    Task<bool> DeleteAsync(int id);
}
