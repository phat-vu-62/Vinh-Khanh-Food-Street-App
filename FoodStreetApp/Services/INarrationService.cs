using FoodStreetApp.Models;

namespace FoodStreetApp.Services
{
    public interface INarrationService
    {
        Task PlayNarrationAsync(POI poi);
        Task StopNarrationAsync();
        Task<bool> IsSpeakingAsync();
    }
}
