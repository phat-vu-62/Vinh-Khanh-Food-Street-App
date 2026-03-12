using FoodStreetApp.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace FoodStreetApp.Views
{
    public partial class MapPage : ContentPage
    {
        private readonly MapPageViewModel _viewModel;
        private readonly Dictionary<int, Pin> _poiPins = new();
        private readonly Dictionary<int, Circle> _poiCircles = new();
        private bool _isMapInitialized = false;
        private bool _isInitializing = false;

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
                }
            }

            // Center map on current location if available
            CenterMapOnCurrentLocation();
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            await _viewModel.CleanupAsync();
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
                    Address = poi.Description,
                    Type = PinType.Place,
                    Location = new Location(poi.Latitude, poi.Longitude)
                };
                pin.MarkerClicked += OnPinMarkerClicked;

                var circle = new Circle
                {
                    Center = new Location(poi.Latitude, poi.Longitude),
                    Radius = new Distance(poi.Radius),
                    StrokeColor = Colors.Blue,
                    StrokeWidth = 2,
                    FillColor = Color.FromArgb("#330000FF")
                };

                map.Pins.Add(pin);
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
        /// Clear and redraw pins and geofence circles for the 5 nearest POIs.
        /// Pulls from the full POI list (not just clustered) so all nearby restaurants
        /// are visible regardless of the spacing filter.
        /// </summary>
        private void RefreshNearbyMapElements(Location location)
        {
            var nearby = _viewModel.GetNearbyPOIs(location, 5);

            map.Pins.Clear();
            map.MapElements.Clear();
            _poiPins.Clear();
            _poiCircles.Clear();

            System.Diagnostics.Debug.WriteLine($"[MAP] Refreshed — {nearby.Count} nearest POIs:");

            foreach (var (poi, distance) in nearby)
            {
                System.Diagnostics.Debug.WriteLine($"[MAP]   '{poi.Name}' — {distance:F0}m (Priority: {poi.Priority})");

                var pin = new Pin
                {
                    Label = poi.Name,
                    Address = $"{poi.Description} ({distance:F0}m away)",
                    Type = PinType.Place,
                    Location = new Location(poi.Latitude, poi.Longitude)
                };
                pin.MarkerClicked += OnPinMarkerClicked;

                var circle = new Circle
                {
                    Center = new Location(poi.Latitude, poi.Longitude),
                    Radius = new Distance(poi.Radius),
                    StrokeColor = Colors.Blue,
                    StrokeWidth = 2,
                    FillColor = Color.FromArgb("#330000FF")
                };

                map.Pins.Add(pin);
                map.MapElements.Add(circle);
                _poiPins[poi.Id] = pin;
                _poiCircles[poi.Id] = circle;
            }
        }

        /// <summary>
        /// Center map on current location
        /// </summary>
        private void CenterMapOnCurrentLocation()
        {
            if (_viewModel.CurrentLocation != null)
            {
                var location = new Location(_viewModel.CurrentLocation.Latitude, _viewModel.CurrentLocation.Longitude);
                map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(300)));
                System.Diagnostics.Debug.WriteLine($">>> Map centered to: {location.Latitude:F6}, {location.Longitude:F6}");
            }
        }

        /// <summary>
        /// Handle pin click to show POI details
        /// </summary>
        private async void OnPinMarkerClicked(object? sender, PinClickedEventArgs e)
        {
            e.HideInfoWindow = true;

            if (sender is Pin pin)
            {
                await DisplayAlert(pin.Label, pin.Address, "OK");
            }
        }

        /// <summary>
        /// Reset all geofences (cooldowns)
        /// </summary>
        private void OnResetClicked(object sender, EventArgs e)
        {
            _viewModel.ResetAllGeofences();
            DisplayAlert("Reset", "All POI cooldowns have been reset", "OK");
        }
    }
}
