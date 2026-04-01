using FoodStreetApp.Api.Interfaces;
using FoodStreetApp.Api.Repositories;
using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.Api.Services;

public class TourService : ITourService
{
    private readonly IRepository<Tour> _repository;

    public TourService(IRepository<Tour> repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<Tour>> GetAllAsync() => _repository.GetAllAsync();

    public Task<Tour?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public Task<Tour> CreateAsync(Tour tour) => _repository.CreateAsync(tour);

    public Task<bool> UpdateAsync(int id, Tour tour) => _repository.UpdateAsync(id, tour);

    public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);
}
