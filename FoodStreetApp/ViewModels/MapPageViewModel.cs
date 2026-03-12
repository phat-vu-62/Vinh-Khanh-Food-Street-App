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
        private POI? _lastTriggeredPoi;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<Location>? LocationChanged; // Event for MapPage to subscribe

        public ObservableCollection<POI> POIs { get; } = new();

        public Location? CurrentLocation
        {
            get => _currentLocation;
            set
            {
                _currentLocation = value;
                OnPropertyChanged();

                // Notify subscribers (MapPage) of location change
                if (value != null)
                {
                    LocationChanged?.Invoke(this, value);
                }
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
                System.Diagnostics.Debug.WriteLine("\n>>> === MAP PAGE INITIALIZATION START ===");

                // Check location permissions first
                StatusMessage = "Checking permissions...";
                var hasPermissions = await CheckLocationPermissionsAsync();

                if (!hasPermissions)
                {
                    StatusMessage = "❌ Location permissions required. Please grant permissions in Settings.";
                    System.Diagnostics.Debug.WriteLine(">>> ❌ Location permissions not granted");
                    return;
                }

                System.Diagnostics.Debug.WriteLine(">>> ✅ Permissions OK");

                StatusMessage = "Loading POIs from database...";
                await _poiService.InitializeAsync();

                var pois = await _poiService.GetActivePOIsAsync();
                System.Diagnostics.Debug.WriteLine($">>> Loaded {pois.Count} POIs from database");

                POIs.Clear();
                foreach (var poi in pois)
                {
                    POIs.Add(poi);
                }

                await _geofenceService.InitializeAsync(pois);

                // After clustering, rebuild the map collection with only the surviving POIs.
                // This prevents map circles from overlapping on dense street segments.
                var clusteredPois = _geofenceService.GetClusteredPOIs();
                POIs.Clear();
                foreach (var poi in clusteredPois)
                {
                    POIs.Add(poi);
                }
                System.Diagnostics.Debug.WriteLine(
                    $"[MAP] Displaying {POIs.Count} clustered POIs on map (of {pois.Count} total)");

                StatusMessage = "Getting location...";
                var location = await _locationService.GetCurrentLocationAsync();
                if (location != null)
                {
                    CurrentLocation = location;
                    UpdateLocationDisplay(location);
                    System.Diagnostics.Debug.WriteLine($">>> Initial location: {location.Latitude:F6}, {location.Longitude:F6}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(">>> ⚠️ Could not get initial location");
                }

                StatusMessage = "Starting tracking...";
                var trackingStarted = await _locationService.StartTrackingAsync();

                if (trackingStarted)
                {
                    StatusMessage = "Exploring Vinh Khanh Street";
                    System.Diagnostics.Debug.WriteLine(">>> ✅ Initialization completed successfully");
                }
                else
                {
                    StatusMessage = "Location tracking failed";
                    System.Diagnostics.Debug.WriteLine(">>> ⚠️ Location tracking failed");
                }

                System.Diagnostics.Debug.WriteLine(">>> === MAP PAGE INITIALIZATION END ===\n");
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($">>> ❌ Initialization error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($">>> Stack trace: {ex.StackTrace}");
            }
        }

        private async Task<bool> CheckLocationPermissionsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(">>> Checking location permissions...");

                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                System.Diagnostics.Debug.WriteLine($">>> LocationWhenInUse status: {status}");

                if (status != PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine(">>> Requesting LocationWhenInUse permission...");
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    System.Diagnostics.Debug.WriteLine($">>> Permission result: {status}");
                }

                if (status == PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine(">>> ✅ Location permissions granted");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($">>> ❌ Location permission denied: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> ❌ Error checking permissions: {ex.Message}");
                return false;
            }
        }

        private void OnLocationChanged(object? sender, Location location)
        {
            System.Diagnostics.Debug.WriteLine($"\n[GPS] 📍 {location.Latitude:F6}, {location.Longitude:F6} (Accuracy: {location.Accuracy:F1}m)");
            CurrentLocation = location;
            UpdateLocationDisplay(location);

            var triggeredPoi = _geofenceService.CheckGeofences(location);
            if (triggeredPoi != null)
            {
                System.Diagnostics.Debug.WriteLine($"[GPS] ✅ Narration triggered: '{triggeredPoi.Name}'");
                StatusMessage = $"Playing: {triggeredPoi.Name}";
                _poiService.UpdatePOIStatus(triggeredPoi);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[GPS] No narration triggered this update");
            }
        }

        private void UpdateLocationDisplay(Location location)
        {
            // Use the same 70m radius as GeofenceService so status text matches narration behaviour
            const double triggerRadius = 70.0;

            CurrentLocationText = $"GPS: {location.Latitude:F6}, {location.Longitude:F6}";

            var nearbyList = GetNearbyPOIs(location, 1);
            if (nearbyList.Count > 0)
            {
                var (poi, distance) = nearbyList[0];
                NearestPoiText = $"{poi.Name} — {distance:F0}m";
                StatusMessage = distance <= triggerRadius
                    ? $"Inside {poi.Name} zone"
                    : $"{distance:F0}m to {poi.Name}";
            }
            else
            {
                NearestPoiText = "No POIs nearby";
                StatusMessage = "Exploring Vinh Khanh Street";
            }
        }

        /// <summary>
        /// Returns up to <paramref name="count"/> nearest active POIs to <paramref name="location"/>,
        /// sorted by ascending distance. Used by MapPage to redraw geofence circles every GPS tick.
        /// </summary>
        public List<(POI poi, double distance)> GetNearbyPOIs(Location location, int count)
            => _geofenceService.GetNearbyPOIs(location, count);

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
