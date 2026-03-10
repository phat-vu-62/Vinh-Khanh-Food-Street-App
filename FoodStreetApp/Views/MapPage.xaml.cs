using FoodStreetApp.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace FoodStreetApp.Views
{
    public partial class MapPage : ContentPage
    {
        private readonly MapPageViewModel _viewModel;

        public MapPage(MapPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await _viewModel.InitializeAsync();

            if (_viewModel.CurrentLocation != null)
            {
                var location = new Location(_viewModel.CurrentLocation.Latitude, _viewModel.CurrentLocation.Longitude);
                map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(0.5)));
            }

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
                map.Pins.Add(pin);

                var circle = new Circle
                {
                    Center = new Location(poi.Latitude, poi.Longitude),
                    Radius = new Distance(poi.Radius),
                    StrokeColor = Colors.Blue,
                    StrokeWidth = 2,
                    FillColor = Colors.Blue.WithAlpha(0.2f)
                };
                map.MapElements.Add(circle);
            }
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            await _viewModel.CleanupAsync();
        }

        private async void OnPinMarkerClicked(object? sender, PinClickedEventArgs e)
        {
            e.HideInfoWindow = true;

            if (sender is Pin pin)
            {
                await DisplayAlert(pin.Label, pin.Address, "OK");
            }
        }

        private void OnResetClicked(object sender, EventArgs e)
        {
            _viewModel.ResetAllGeofences();
        }
    }
}
