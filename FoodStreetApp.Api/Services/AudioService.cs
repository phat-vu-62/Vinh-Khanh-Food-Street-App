using FoodStreetApp.Api.Interfaces;
using FoodStreetApp.Api.Repositories;
using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.Api.Services;

public class AudioService : IAudioService
{
    private readonly IRepository<Audio> _repository;

    public AudioService(IRepository<Audio> repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<Audio>> GetAllAsync() => _repository.GetAllAsync();

    public Task<Audio?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public Task<Audio> CreateAsync(Audio audio) => _repository.CreateAsync(audio);

    public Task<bool> UpdateAsync(int id, Audio audio) => _repository.UpdateAsync(id, audio);

    public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);
}
