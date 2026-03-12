using FoodStreetApp.Models;

namespace FoodStreetApp.Services
{
    public interface IGeofenceService
    {
        Task InitializeAsync(List<POI> pois);
        POI? CheckGeofences(Location currentLocation);
        void ResetAllGeofences();
        void ResetGeofence(int poiId);

        /// <summary>
        /// Returns the POI list after the clustering filter has been applied.
        /// Use this for map display to avoid showing overlapping markers.
        /// </summary>
        List<POI> GetClusteredPOIs();

        /// <summary>
        /// Returns the <paramref name="count"/> nearest active POIs to <paramref name="location"/>,
        /// sorted by ascending distance, from the full (non-clustered) POI list.
        /// </summary>
        List<(POI poi, double distance)> GetNearbyPOIs(Location location, int count);
    }
}
