using System.Windows.Input;
using FoodStreetApp.Models;
using FoodStreetApp.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FoodStreetApp.ViewModels
{
    [QueryProperty(nameof(Place), "FoodPlace")]
    public class FoodDetailViewModel : INotifyPropertyChanged
    {
        private readonly INarrationService _narrationService;
        private FoodPlace? _place;
        private string _playStatus = "Play";

        public event PropertyChangedEventHandler? PropertyChanged;

        public FoodPlace? Place
        {
            get => _place;
            set
            {
                _place = value;
                PlayStatus = "Play";
                OnPropertyChanged();
            }
        }

        public string PlayStatus
        {
            get => _playStatus;
            set
            {
                _playStatus = value;
                OnPropertyChanged();
            }
        }

        public ICommand ShowOnMapCommand { get; }
        public ICommand ToggleNarrationCommand { get; }

        public FoodDetailViewModel(INarrationService narrationService)
        {
            _narrationService = narrationService;
            _narrationService.NarrationFinished += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (PlayStatus == "Stop")
                    {
                        PlayStatus = "Replay";
                    }
                });
            };

            ShowOnMapCommand = new Command(async () =>
            {
                if (Place == null) return;

                string latStr = Place.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lonStr = Place.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                // Navigate to Map tab and pass coordinates
                await Shell.Current.GoToAsync($"///MapPage?lat={latStr}&lon={lonStr}");
            });

            ToggleNarrationCommand = new Command(() =>
            {
                if (Place == null) return;

                var poi = Place.PoiData ?? new POI 
                { 
                    Name = Place.Name, 
                    Description = Place.Description, 
                    UseTts = true, 
                    TtsText = Place.Description
                };

                if (PlayStatus == "Play" || PlayStatus == "Replay")
                {
                    PlayStatus = "Stop";
                    _ = _narrationService.PlayNarrationAsync(poi, isManual: true);
                }
                else if (PlayStatus == "Stop")
                {
                    _ = _narrationService.StopNarrationAsync();
                    PlayStatus = "Play";
                }
            });
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}