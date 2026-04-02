using FoodStreetApp.Models;

namespace FoodStreetApp.Services
{
    public interface IPOIService
    {
        Task InitializeAsync();
        Task<int> SyncFromWebAsync(string? syncUrl = null);
        Task<List<POI>> GetAllPOIsAsync();
        Task<List<POI>> GetActivePOIsAsync();
        void UpdatePOIStatus(POI poi);
        void ResetAllPOIStatus();
    }
}
