using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.Api.Interfaces;

public interface IAudioService
{
    Task<IReadOnlyCollection<Audio>> GetAllAsync();
    Task<Audio?> GetByIdAsync(int id);
    Task<Audio> CreateAsync(Audio audio);
    Task<bool> UpdateAsync(int id, Audio audio);
    Task<bool> DeleteAsync(int id);
}
