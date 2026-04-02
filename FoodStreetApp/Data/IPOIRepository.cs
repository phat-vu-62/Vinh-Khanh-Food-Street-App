using FoodStreetApp.Models;

namespace FoodStreetApp.Data
{
    public interface IPOIRepository
    {
        Task InitializeAsync();
        Task<List<POI>> GetAllPOIsAsync();
        Task<List<POI>> GetActivePOIsAsync();
        Task<POI?> GetPOIByIdAsync(int id);
        Task<int> SavePOIAsync(POI poi);
        Task<int> DeletePOIAsync(int id);
        Task<int> UpdatePOIAsync(POI poi);
        Task SeedDataAsync();
        Task<int> SyncFromWebAsync(string? syncUrl = null);
    }
}
