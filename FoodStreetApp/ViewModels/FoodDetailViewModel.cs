using System.Windows.Input;
using FoodStreetApp.Models;
using FoodStreetApp.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FoodStreetApp.ViewModels
{
    [QueryProperty(nameof(Place), "FoodPlace")]
    [QueryProperty(nameof(AutoPlay), "AutoPlay")]
    [QueryProperty(nameof(SkipGps), "SkipGps")]
    public class FoodDetailViewModel : INotifyPropertyChanged
    {
        private readonly INarrationService _narrationService;
        private FoodPlace? _place;
        private string _playStatus = "Play";
        private string? _autoPlay;
        private string? _skipGps;

        public event PropertyChangedEventHandler? PropertyChanged;

        public FoodPlace? Place
        {
            get => _place;
            set
            {
                _place = value;
                PlayStatus = LocalizationResourceManager.Instance["Play"];
                OnPropertyChanged();

                // If arriving from QR with AutoPlay=true, start playback immediately
                if (_autoPlay == "True" || _autoPlay == "true")
                {
                    // Reset to avoid loops if property is reset
                    _autoPlay = "false";
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (ToggleNarrationCommand.CanExecute(null))
                            ToggleNarrationCommand.Execute(null);
                    });
                }
            }
        }

        public string? AutoPlay { get => _autoPlay; set { _autoPlay = value; OnPropertyChanged(); } }
        public string? SkipGps { get => _skipGps; set { _skipGps = value; OnPropertyChanged(); } }

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
                    if (PlayStatus == LocalizationResourceManager.Instance["Stop"])
                    {
                        PlayStatus = LocalizationResourceManager.Instance["Replay"];
                    }
                });
            };

            ShowOnMapCommand = new Command(async () =>
            {
                if (Place == null) return;

                string latStr = Place.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lonStr = Place.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string nameEncoded = Uri.EscapeDataString(Place.Name);

                // Navigate to Map tab (which is at the root) and pass coordinates
                await Shell.Current.GoToAsync($"///MapPage?lat={latStr}&lon={lonStr}&name={nameEncoded}");
            });

            ToggleNarrationCommand = new Command(() =>
            {
                if (Place == null) return;

                var playText = LocalizationResourceManager.Instance["Play"];
                var stopText = LocalizationResourceManager.Instance["Stop"];
                var replayText = LocalizationResourceManager.Instance["Replay"];

                if (PlayStatus == playText || PlayStatus == replayText)
                {
                    PlayStatus = stopText;

                    var poi = Place.PoiData ?? new POI 
                    { 
                        Name = Place.Name, 
                        Description = Place.Description, 
                        UseTts = true, 
                        TtsText = Place.Description 
                    };

                    _ = _narrationService.PlayNarrationAsync(poi, isManual: true);
                }
                else if (PlayStatus == stopText)
                {
                    _ = _narrationService.StopNarrationAsync();
                    PlayStatus = playText;
                }
            });

            // Update translation if language changes while on this page
            LocalizationResourceManager.Instance.PropertyChanged += (s, e) =>
            {
                if (PlayStatus == "Play" || PlayStatus == "Phát" || PlayStatus == "재생" || PlayStatus == "再生" || PlayStatus == "播放") PlayStatus = LocalizationResourceManager.Instance["Play"];
                if (PlayStatus == "Stop" || PlayStatus == "Dừng" || PlayStatus == "정지" || PlayStatus == "停止" || PlayStatus == "停止") PlayStatus = LocalizationResourceManager.Instance["Stop"];
                if (PlayStatus == "Replay" || PlayStatus == "Phát lại" || PlayStatus == "다시 재생" || PlayStatus == "リプレイ" || PlayStatus == "重播") PlayStatus = LocalizationResourceManager.Instance["Replay"];
            };
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}