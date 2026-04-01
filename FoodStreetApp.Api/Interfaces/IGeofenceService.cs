namespace FoodStreetApp.Api.Interfaces;

public interface IGeofenceService
{
    bool IsInsideGeofence(double userLatitude, double userLongitude, double targetLatitude, double targetLongitude, int radiusMeters);
}
