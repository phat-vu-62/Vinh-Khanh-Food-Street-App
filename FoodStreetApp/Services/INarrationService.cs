using FoodStreetApp.Models;

namespace FoodStreetApp.Services
{
    public interface INarrationService
    {
        Task PlayNarrationAsync(POI poi, bool isManual = false);
        Task StopNarrationAsync();
        Task<bool> IsSpeakingAsync();
        event EventHandler NarrationFinished;
    }
}
