using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.Api.Interfaces;

public interface ITourService
{
    Task<IReadOnlyCollection<Tour>> GetAllAsync();
    Task<Tour?> GetByIdAsync(int id);
    Task<Tour> CreateAsync(Tour tour);
    Task<bool> UpdateAsync(int id, Tour tour);
    Task<bool> DeleteAsync(int id);
}
