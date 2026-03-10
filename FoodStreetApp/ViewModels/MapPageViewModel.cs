using FoodStreetApp.Services;
using FoodStreetApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FoodStreetApp.ViewModels
{
    public class MapPageViewModel : INotifyPropertyChanged
    {
        private readonly ILocationService _locationService;
        private readonly IGeofenceService _geofenceService;
        private readonly IPOIService _poiService;
        private readonly INarrationService _narrationService;

        private Location? _currentLocation;
        private string _statusMessage = "Initializing...";
        private string _currentLocationText = "📍 GPS: Acquiring location...";
        private string _nearestPoiText = "🎯 Nearest POI: Searching...";

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<POI> POIs { get; } = new();

        public Location? CurrentLocation
        {
            get => _currentLocation;
            set
            {
                _currentLocation = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public string CurrentLocationText
        {
            get => _currentLocationText;
            set
            {
                _currentLocationText = value;
                OnPropertyChanged();
            }
        }

        public string NearestPoiText
        {
            get => _nearestPoiText;
            set
            {
                _nearestPoiText = value;
                OnPropertyChanged();
            }
        }

        public MapPageViewModel(
            ILocationService locationService,
            IGeofenceService geofenceService,
            IPOIService poiService,
            INarrationService narrationService)
        {
            _locationService = locationService;
            _geofenceService = geofenceService;
            _poiService = poiService;
            _narrationService = narrationService;

            _locationService.LocationChanged += OnLocationChanged;
        }

        public async Task InitializeAsync()
        {
            try
            {
                StatusMessage = "Loading POIs from database...";
                await _poiService.InitializeAsync();

                var pois = await _poiService.GetActivePOIsAsync();
                POIs.Clear();
                foreach (var poi in pois)
                {
                    POIs.Add(poi);
                }

                await _geofenceService.InitializeAsync(pois);

                StatusMessage = "Getting GPS location...";
                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    CurrentLocation = location;
                    UpdateLocationDisplay(location);
                }

                StatusMessage = "Starting location tracking...";
                await _locationService.StartTrackingAsync();
                StatusMessage = "Ready - Exploring Vinh Khanh Street";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Initialization error: {ex}");
            }
        }

        private void OnLocationChanged(object? sender, Location location)
        {
            CurrentLocation = location;
            UpdateLocationDisplay(location);

            var triggeredPoi = _geofenceService.CheckGeofences(location);
            if (triggeredPoi != null)
            {
                StatusMessage = $"🎵 Playing: {triggeredPoi.Name}";
                _poiService.UpdatePOIStatus(triggeredPoi);
            }
        }

        private void UpdateLocationDisplay(Location location)
        {
            CurrentLocationText = $"📍 {location.Latitude:F6}, {location.Longitude:F6}";

            var nearestPoi = FindNearestPOI(location);
            if (nearestPoi.poi != null)
            {
                NearestPoiText = $"🎯 {nearestPoi.poi.Name} - {nearestPoi.distance:F0}m (Priority: {nearestPoi.poi.Priority})";

                if (nearestPoi.distance < nearestPoi.poi.Radius)
                {
                    StatusMessage = $"✅ Inside {nearestPoi.poi.Name} zone";
                }
                else
                {
                    StatusMessage = $"Walk {nearestPoi.distance:F0}m to {nearestPoi.poi.Name}";
                }
            }
            else
            {
                NearestPoiText = "No POIs nearby";
                StatusMessage = "Exploring Vinh Khanh Street";
            }
        }

        private (POI? poi, double distance) FindNearestPOI(Location location)
        {
            POI? nearestPoi = null;
            double minDistance = double.MaxValue;

            foreach (var poi in POIs.Where(p => p.IsActive))
            {
                var distance = CalculateDistance(location.Latitude, location.Longitude, poi.Latitude, poi.Longitude);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPoi = poi;
                }
            }

            return (nearestPoi, minDistance);
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371.0;
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c * 1000;
        }

        private double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

        public async Task CleanupAsync()
        {
            await _locationService.StopTrackingAsync();
            await _narrationService.StopNarrationAsync();
        }

        public void ResetAllGeofences()
        {
            _geofenceService.ResetAllGeofences();
            _poiService.ResetAllPOIStatus();
            StatusMessage = "All geofences reset";
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
