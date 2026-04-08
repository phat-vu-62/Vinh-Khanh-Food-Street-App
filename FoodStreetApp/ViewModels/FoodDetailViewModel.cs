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
    [QueryProperty(nameof(QRCode), "QRCode")]
    public class FoodDetailViewModel : INotifyPropertyChanged
    {
        private readonly INarrationService _narrationService;
        private readonly ITrackingService _trackingService;
        private string? _qrCode;


        private FoodPlace? _place;
        private string _playStatus = "Play";
        private string? _autoPlay;
        private string? _skipGps;
        private bool _poiViewTracked = false;
        private bool _qrScannedTracked = false;

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
                    _autoPlay = "false";
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (ToggleNarrationCommand.CanExecute(null))
                            ToggleNarrationCommand.Execute(null);
                    });
                }

                // Trigger unified tracking check
                _ = TryTrackInteractionAsync();
            }
        }

        public string? AutoPlay { get => _autoPlay; set { _autoPlay = value; OnPropertyChanged(); } }
        public string? SkipGps { get => _skipGps; set { _skipGps = value; OnPropertyChanged(); } }
        public string? QRCode 
        { 
            get => _qrCode; 
            set 
            { 
                _qrCode = value; 
                OnPropertyChanged(); 
                
                // Trigger unified tracking check
                _ = TryTrackInteractionAsync();
            } 
        }

        /// <summary>
        /// Unified method to handle tracking triggers. 
        /// Resolves race conditions during Shell navigation property assignment.
        /// </summary>
        private async Task TryTrackInteractionAsync()
        {
            // We need the POI data before we can track anything
            if (_place?.PoiData == null) return;

            var poi = _place.PoiData;

            // 1. Handle POI View Tracking (Once per page instance)
            if (!_poiViewTracked)
            {
                _poiViewTracked = true;
                
                // MANDATORY LOGGING: Identify EXACTLY which POI is being tracked
                Console.WriteLine($"[TRACKING] POI: {poi.Id} - {poi.Name}");
                System.Diagnostics.Debug.WriteLine($"[TRACKING] VIEW: {poi.Id} - {poi.Name}");
                
                await _trackingService.TrackEventAsync(poi.Id, "poi_viewed");
            }

            // 2. Handle QR Scan Tracking (Once if QRCode is present)
            if (!_qrScannedTracked && !string.IsNullOrEmpty(_qrCode))
            {
                _qrScannedTracked = true;
                
                System.Diagnostics.Debug.WriteLine($"[TRACKING] QR SCAN: {poi.Id} (URI: {_qrCode})");
                
                await _trackingService.TrackEventAsync(poi.Id, "qr_scanned", qrCode: _qrCode);
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

        public FoodDetailViewModel(INarrationService narrationService, ITrackingService trackingService)
        {
            _narrationService = narrationService;
            _trackingService = trackingService;
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