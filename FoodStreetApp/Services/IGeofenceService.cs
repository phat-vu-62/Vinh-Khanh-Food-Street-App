using FoodStreetApp.Models;

namespace FoodStreetApp.Services
{
    public interface IGeofenceService
    {
        Task InitializeAsync(List<POI> pois);
        POI? CheckGeofences(Location currentLocation);
        void ResetAllGeofences();
        void ResetGeofence(int poiId);
    }
}
