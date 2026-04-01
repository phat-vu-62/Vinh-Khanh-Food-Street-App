using FoodStreetApp.Api.Interfaces;
using FoodStreetApp.Api.Repositories;
using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.Api.Services;

public class POIService : IPOIService
{
    private readonly IRepository<POI> _repository;

    public POIService(IRepository<POI> repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<POI>> GetAllAsync() => _repository.GetAllAsync();

    public Task<POI?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public Task<POI> CreateAsync(POI poi) => _repository.CreateAsync(poi);

    public Task<bool> UpdateAsync(int id, POI poi) => _repository.UpdateAsync(id, poi);

    public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);
}
