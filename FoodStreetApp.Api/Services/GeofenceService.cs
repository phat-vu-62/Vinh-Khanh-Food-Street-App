using FoodStreetApp.Api.Interfaces;

namespace FoodStreetApp.Api.Services;

public class GeofenceService : IGeofenceService
{
    public bool IsInsideGeofence(double userLatitude, double userLongitude, double targetLatitude, double targetLongitude, int radiusMeters)
    {
        const double metersPerDegree = 111_139d;
        var deltaLat = userLatitude - targetLatitude;
        var deltaLng = userLongitude - targetLongitude;
        var distance = Math.Sqrt((deltaLat * deltaLat) + (deltaLng * deltaLng)) * metersPerDegree;
        return distance <= radiusMeters;
    }
}
