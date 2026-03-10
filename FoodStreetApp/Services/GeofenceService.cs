using FoodStreetApp.Models;

namespace FoodStreetApp.Services
{
    public class GeofenceService : IGeofenceService
    {
        private List<POI> _pois = new();
        private readonly INarrationService _narrationService;

        public GeofenceService(INarrationService narrationService)
        {
            _narrationService = narrationService;
        }

        public Task InitializeAsync(List<POI> pois)
        {
            _pois = pois;
            System.Diagnostics.Debug.WriteLine($"GeofenceService initialized with {_pois.Count} POIs");
            return Task.CompletedTask;
        }

        public POI? CheckGeofences(Location currentLocation)
        {
            var now = DateTime.UtcNow;
            POI? triggeredPoi = null;
            double minDistance = double.MaxValue;

            System.Diagnostics.Debug.WriteLine($"--- Geofence Check: {currentLocation.Latitude:F6}, {currentLocation.Longitude:F6} ---");

            foreach (var poi in _pois.Where(p => p.IsActive).OrderByDescending(p => p.Priority))
            {
                var distance = CalculateDistanceInMeters(
                    currentLocation.Latitude,
                    currentLocation.Longitude,
                    poi.Latitude,
                    poi.Longitude);

                var cooldownExpired = !poi.LastTriggered.HasValue ||
                                     (now - poi.LastTriggered.Value).TotalSeconds >= poi.CooldownSeconds;

                System.Diagnostics.Debug.WriteLine(
                    $"POI: {poi.Name} | Distance: {distance:F2}m | Radius: {poi.Radius}m | " +
                    $"Priority: {poi.Priority} | CooldownOK: {cooldownExpired}");

                if (distance < poi.Radius && cooldownExpired && distance < minDistance)
                {
                    minDistance = distance;
                    triggeredPoi = poi;
                }
            }

            if (triggeredPoi != null)
            {
                System.Diagnostics.Debug.WriteLine($">>> GEOFENCE TRIGGERED: {triggeredPoi.Name} (Priority: {triggeredPoi.Priority})");
                triggeredPoi.LastTriggered = now;
                triggeredPoi.HasPlayed = true;

                _ = _narrationService.PlayNarrationAsync(triggeredPoi);
            }

            return triggeredPoi;
        }

        public void ResetAllGeofences()
        {
            foreach (var poi in _pois)
            {
                poi.HasPlayed = false;
                poi.LastTriggered = null;
            }
            System.Diagnostics.Debug.WriteLine("All geofences reset");
        }

        public void ResetGeofence(int poiId)
        {
            var poi = _pois.FirstOrDefault(p => p.Id == poiId);
            if (poi != null)
            {
                poi.HasPlayed = false;
                poi.LastTriggered = null;
                System.Diagnostics.Debug.WriteLine($"Geofence reset for: {poi.Name}");
            }
        }

        private double CalculateDistanceInMeters(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            const double earthRadiusKm = 6371.0;

            var lat1Rad = DegreesToRadians(latitude1);
            var lat2Rad = DegreesToRadians(latitude2);
            var deltaLat = DegreesToRadians(latitude2 - latitude1);
            var deltaLon = DegreesToRadians(longitude2 - longitude1);

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            var distanceKm = earthRadiusKm * c;
            return distanceKm * 1000;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
