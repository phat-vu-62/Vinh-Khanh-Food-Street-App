using FoodStreetApp.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace FoodStreetApp.Views
{
    public partial class MapPage : ContentPage, IQueryAttributable
    {
        private readonly MapPageViewModel _viewModel;
        private readonly Dictionary<int, Pin> _poiPins = new();
        private readonly Dictionary<int, Circle> _poiCircles = new();
        private bool _isMapInitialized = false;
        private bool _isInitializing = false;
        private readonly HashSet<int> _shownPoiIds = new();
        private Location? _targetLocation;
        private Pin? _targetPin;

        public MapPage(MapPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            // Subscribe to location changes for map centering
            _viewModel.LocationChanged += OnViewModelLocationChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Initialize only once, but retry if a previous attempt failed (e.g. permission denied)
            if (!_isMapInitialized && !_isInitializing)
            {
                _isInitializing = true;
                var success = await _viewModel.InitializeAsync();
                _isInitializing = false;

                if (success)
                {
                    InitializeMapElements();
                    _isMapInitialized = true;
                    // Re-assert after permission grant so Android activates the My Location button
                    map.IsShowingUser = true;
                }
            }
            else if (_isMapInitialized)
            {
                // Page reappeared after being hidden — restart tracking that was stopped on disappear
                await _viewModel.RestartTrackingAsync();
            }

            // Center map on current location if available
            CenterMapOnCurrentLocation();
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            await _viewModel.CleanupAsync();

            MarkerInfoCard.IsVisible = false;

            if (_targetPin != null && map.Pins.Contains(_targetPin))
            {
                map.Pins.Remove(_targetPin);
                _targetPin = null;
            }
        }

        /// <summary>
        /// Draw initial POI markers and geofence circles.
        /// Uses the 5 nearest POIs if a location is already available; falls back to the
        /// clustered POI list so the map is never blank at startup.
        /// </summary>
        private void InitializeMapElements()
        {
            if (_viewModel.CurrentLocation != null)
            {
                System.Diagnostics.Debug.WriteLine("[MAP] Initial draw via RefreshNearbyMapElements (location available)");
                RefreshNearbyMapElements(_viewModel.CurrentLocation);
                return;
            }

            // No GPS fix yet — draw all clustered POIs as a static fallback.
            map.Pins.Clear();
            map.MapElements.Clear();
            _poiPins.Clear();
            _poiCircles.Clear();

            System.Diagnostics.Debug.WriteLine($"[MAP] Initial static draw: {_viewModel.POIs.Count} clustered POIs (no location yet)");

            foreach (var poi in _viewModel.POIs)
            {
                var pin = new Pin
                {
                    Label = poi.Name,
                    Address = FoodStreetApp.Services.LocalizationResourceManager.Instance[poi.Description],
                    Type = PinType.Place,
                    Location = new Location(poi.Latitude, poi.Longitude)
                };
                pin.MarkerClicked += OnPinMarkerClicked;

                var circle = new Circle
                {
                    Center = new Location(poi.Latitude, poi.Longitude),
                    Radius = new Distance(poi.Radius),
                    StrokeColor = Colors.Transparent,
                    StrokeWidth = 0,
                    FillColor = Colors.Transparent
                };

                // Do not add pin initially, it will be added when user gets close
                map.MapElements.Add(circle);
                _poiPins[poi.Id] = pin;
                _poiCircles[poi.Id] = circle;

                System.Diagnostics.Debug.WriteLine($"[MAP] Added POI: {poi.Name} (Priority: {poi.Priority}, Radius: {poi.Radius}m)");
            }
        }

        /// <summary>
        /// On each GPS tick, redraw the 5 nearest POI circles so the map always reflects
        /// the user's current surroundings.
        /// </summary>
        private void OnViewModelLocationChanged(object? sender, Location location)
        {
            MainThread.BeginInvokeOnMainThread(() => RefreshNearbyMapElements(location));
        }

        /// <summary>
        /// Refresh pins and geofence circles for the 5 nearest POIs.
        /// Pulls from the full POI list (not just clustered) so all nearby restaurants
        /// are visible regardless of the spacing filter.
        /// When the set of visible POIs has not changed, only distance labels are updated
        /// to avoid the cost of clearing and rebuilding the map every GPS tick.
        /// </summary>
        private void RefreshNearbyMapElements(Location location)
        {
            var nearby = _viewModel.GetNearbyPOIs(location, 50);
            var newIds = new HashSet<int>(nearby.Select(x => x.poi.Id));

            if (newIds.SetEquals(_shownPoiIds))
            {
                // Same POIs still in view — only update the distance labels and circle colors
                foreach (var (poi, distance) in nearby)
                {
                    bool isInside = distance <= 18.0;

                    if (_poiPins.TryGetValue(poi.Id, out var existingPin))
                    {
                        existingPin.Address = FoodStreetApp.Services.LocalizationResourceManager.Instance[poi.Description];

                        bool pinExistsOnMap = map.Pins.Contains(existingPin);
                        if (isInside && !pinExistsOnMap)
                        {
                            map.Pins.Add(existingPin);
                        }
                        else if (!isInside && pinExistsOnMap)
                        {
                            map.Pins.Remove(existingPin);
                        }
                    }

                    if (_poiCircles.TryGetValue(poi.Id, out var existingCircle))
                    {
                        if (isInside) // POI_TRIGGER_RADIUS
                        {
                            existingCircle.StrokeWidth = 2;
                            existingCircle.StrokeColor = Colors.Red;
                            existingCircle.FillColor = Color.FromArgb("#33FF0000");
                        }
                        else
                        {
                            existingCircle.StrokeWidth = 0;
                            existingCircle.StrokeColor = Colors.Transparent;
                            existingCircle.FillColor = Colors.Transparent;
                        }
                    }
                }

                if (_targetPin != null && MarkerInfoCard.IsVisible && !map.Pins.Contains(_targetPin))
                {
                    map.Pins.Add(_targetPin);
                }

                return;
            }

            // Visible set changed — full clear and redraw
            map.Pins.Clear();
            map.MapElements.Clear();
            _poiPins.Clear();
            _poiCircles.Clear();
            _shownPoiIds.Clear();

            System.Diagnostics.Debug.WriteLine($"[MAP] Refreshed — {nearby.Count} nearest POIs:");

            foreach (var (poi, distance) in nearby)
            {
                System.Diagnostics.Debug.WriteLine($"[MAP]   '{poi.Name}' — {distance:F0}m (Priority: {poi.Priority})");

                bool isInside = distance <= 18.0;

                var pin = new Pin
                {
                    Label = poi.Name,
                    Address = FoodStreetApp.Services.LocalizationResourceManager.Instance[poi.Description],
                    Type = PinType.Place,
                    Location = new Location(poi.Latitude, poi.Longitude)
                };
                pin.MarkerClicked += OnPinMarkerClicked;

                if (isInside)
                {
                    map.Pins.Add(pin);
                }

                var circle = new Circle
                {
                    Center = new Location(poi.Latitude, poi.Longitude),
                    Radius = new Distance(poi.Radius),
                    StrokeColor = isInside ? Colors.Red : Colors.Transparent,
                    StrokeWidth = isInside ? 2 : 0,
                    FillColor = isInside ? Color.FromArgb("#33FF0000") : Colors.Transparent
                };

                map.MapElements.Add(circle);
                _poiPins[poi.Id] = pin;
                _poiCircles[poi.Id] = circle;
                _shownPoiIds.Add(poi.Id);
            }

            if (_targetPin != null && MarkerInfoCard.IsVisible)
            {
                map.Pins.Add(_targetPin);
            }
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("lat") && query.ContainsKey("lon"))
            {
                if (double.TryParse(query["lat"].ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(query["lon"].ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lon))
                {
                    var targetLoc = new Location(lat, lon);
                    _targetLocation = targetLoc;

                    string targetName = query.ContainsKey("name") ? Uri.UnescapeDataString(query["name"].ToString() ?? "") : string.Empty;

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        while (map.Width <= 0 || map.Height <= 0)
                        {
                            await Task.Delay(100);
                        }
                        await Task.Delay(300);

                        // Mức zoom gần hơn (50m thay vì 100m) giống Google Maps
                        map.MoveToRegion(MapSpan.FromCenterAndRadius(targetLoc, Distance.FromMeters(50)));
                        System.Diagnostics.Debug.WriteLine($">>> Map centered to Target via QueryAttributes: {targetLoc.Latitude:F6}, {targetLoc.Longitude:F6}");

                        if (_targetPin != null && map.Pins.Contains(_targetPin))
                        {
                            map.Pins.Remove(_targetPin);
                        }

                        // Hiển thị Card thông tin quán ngay lập tức
                        if (!string.IsNullOrEmpty(targetName))
                        {
                            var poi = await _viewModel.GetPOIByNameAsync(targetName);
                            if (poi != null)
                            {
                                MarkerTitleLabel.Text = poi.Name;
                                MarkerDescLabel.Text = FoodStreetApp.Services.LocalizationResourceManager.Instance[poi.Description];
                                MarkerInfoCard.IsVisible = true;

                                _targetPin = new Pin
                                {
                                    Label = poi.Name,
                                    Address = FoodStreetApp.Services.LocalizationResourceManager.Instance[poi.Description],
                                    Type = PinType.Place,
                                    Location = new Location(poi.Latitude, poi.Longitude)
                                };
                                _targetPin.MarkerClicked += OnPinMarkerClicked;
                                map.Pins.Add(_targetPin);
                            }
                        }

                        _targetLocation = null;
                    });
                }
            }
        }

        /// <summary>
        /// Center map on current location or target location from navigation
        /// </summary>
        private void CenterMapOnCurrentLocation()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Ensure map is rendered (width/height > 0) to avoid silent failure on MoveToRegion
                if (map.Width <= 0 || map.Height <= 0)
                {
                    await Task.Delay(500);
                }

                if (_targetLocation != null)
                {
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(_targetLocation, Distance.FromMeters(100)));
                    System.Diagnostics.Debug.WriteLine($">>> Map centered to Target: {_targetLocation.Latitude:F6}, {_targetLocation.Longitude:F6}");
                    _targetLocation = null; // Reset after using it once
                }
                else if (_viewModel.CurrentLocation != null)
                {
                    var location = new Location(_viewModel.CurrentLocation.Latitude, _viewModel.CurrentLocation.Longitude);
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(300)));
                    System.Diagnostics.Debug.WriteLine($">>> Map centered to: {location.Latitude:F6}, {location.Longitude:F6}");
                }
                else
                {
                    var vinhKhanh = new Location(10.7610, 106.7040);
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(vinhKhanh, Distance.FromMeters(250)));
                    System.Diagnostics.Debug.WriteLine(">>> Map centered to Vinh Khanh Street (no GPS fix yet)");
                }
            });
        }

        /// <summary>
        /// Handle pin click to show POI details
        /// </summary>
        private void OnPinMarkerClicked(object? sender, PinClickedEventArgs e)
        {
            e.HideInfoWindow = true;
            if (sender is Pin pin)
            {
                MarkerTitleLabel.Text = pin.Label;
                MarkerDescLabel.Text = pin.Address;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    MarkerInfoCard.IsVisible = true;
                });
            }
        }

        private void OnCloseMarkerInfoClicked(object sender, EventArgs e)
        {
            MarkerInfoCard.IsVisible = false;
            
            if (_targetPin != null && map.Pins.Contains(_targetPin))
            {
                map.Pins.Remove(_targetPin);
                _targetPin = null;
            }
        }

        /// <summary>
        /// Reset all geofences (cooldowns)
        /// </summary>
        private void OnResetClicked(object sender, EventArgs e)
        {
            _shownPoiIds.Clear(); // Force full map redraw on next GPS tick
            _viewModel.ResetAllGeofences();
            DisplayAlert(FoodStreetApp.Services.LocalizationResourceManager.Instance["Reset"], FoodStreetApp.Services.LocalizationResourceManager.Instance["All POI cooldowns have been reset"], FoodStreetApp.Services.LocalizationResourceManager.Instance["OK"]);
        }
    }
}
