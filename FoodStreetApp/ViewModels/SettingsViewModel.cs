using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.ObjectModel;
using FoodStreetApp.Services;
using FoodStreetApp.Models;

namespace FoodStreetApp.ViewModels
{
    public class LanguageOption
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NativeName { get; set; } = string.Empty;
    }

    public class SettingsViewModel : INotifyPropertyChanged
    {
        private const string KEY_UPDATE_FREQUENCY = "update_frequency";
        private const string KEY_DEFAULT_RADIUS = "default_radius";
        private const string KEY_COOLDOWN_MINUTES = "cooldown_minutes";
        private const string KEY_PREFER_TTS = "prefer_tts";
        private const string KEY_ENABLE_AUDIO = "enable_audio";
        private const string KEY_BACKGROUND_TRACKING = "background_tracking";
        private const string KEY_APP_LANGUAGE = "app_language";

        private readonly INarrationService? _narrationService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand TestTtsCommand { get; }
        public ICommand TestNarrationCommand { get; }
        public ICommand TestGpsCommand { get; }
        public ObservableCollection<LanguageOption> AvailableLanguages { get; }

        private LanguageOption? _selectedLanguage;
        public LanguageOption? SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value;
                if (value != null)
                {
                    Preferences.Set(KEY_APP_LANGUAGE, value.Code);
                    (_narrationService as NarrationService)?.SetLanguage(value.Code);
                    System.Diagnostics.Debug.WriteLine($"Language changed to: {value.Name} ({value.Code})");
                }
                OnPropertyChanged();
            }
        }

        private int _updateFrequency;
        public int UpdateFrequency
        {
            get => _updateFrequency;
            set
            {
                _updateFrequency = value;
                Preferences.Set(KEY_UPDATE_FREQUENCY, value);
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine($"Settings: Update frequency set to {value}s");
            }
        }

        private int _defaultRadius;
        public int DefaultRadius
        {
            get => _defaultRadius;
            set
            {
                _defaultRadius = value;
                Preferences.Set(KEY_DEFAULT_RADIUS, value);
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine($"Settings: Default radius set to {value}m");
            }
        }

        private int _cooldownMinutes;
        public int CooldownMinutes
        {
            get => _cooldownMinutes;
            set
            {
                _cooldownMinutes = value;
                Preferences.Set(KEY_COOLDOWN_MINUTES, value);
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine($"Settings: Cooldown set to {value} minutes");
            }
        }

        private bool _preferTts;
        public bool PreferTts
        {
            get => _preferTts;
            set
            {
                _preferTts = value;
                Preferences.Set(KEY_PREFER_TTS, value);
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine($"Settings: Prefer TTS = {value}");
            }
        }

        private bool _enableAudio;
        public bool EnableAudio
        {
            get => _enableAudio;
            set
            {
                _enableAudio = value;
                Preferences.Set(KEY_ENABLE_AUDIO, value);
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine($"Settings: Enable audio = {value}");
            }
        }

        private bool _enableBackgroundTracking;
        public bool EnableBackgroundTracking
        {
            get => _enableBackgroundTracking;
            set
            {
                _enableBackgroundTracking = value;
                Preferences.Set(KEY_BACKGROUND_TRACKING, value);
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine($"Settings: Background tracking = {value}");
                
                if (value)
                {
                    StartBackgroundService();
                }
                else
                {
                    StopBackgroundService();
                }
            }
        }

        public SettingsViewModel(INarrationService narrationService)
        {
            _narrationService = narrationService;
            TestTtsCommand = new Command(async () => await TestTts());
            TestNarrationCommand = new Command(async () => await TestNarration());
            TestGpsCommand = new Command(async () => await TestGps());

            // Initialize language options
            AvailableLanguages = new ObservableCollection<LanguageOption>
            {
                new LanguageOption { Code = "vi", Name = "Vietnamese", NativeName = "Tiếng Việt" },
                new LanguageOption { Code = "en", Name = "English", NativeName = "English" },
                new LanguageOption { Code = "ko", Name = "Korean", NativeName = "한국어" },
                new LanguageOption { Code = "zh", Name = "Chinese", NativeName = "中文" },
                new LanguageOption { Code = "ja", Name = "Japanese", NativeName = "日本語" }
            };

            LoadSettings();
        }

        private async Task TestGps()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("\n>>> ===== GPS TEST STARTED =====");
                System.Diagnostics.Debug.WriteLine(">>> Requesting current location...");

                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> ✅ GPS TEST SUCCESS!");
                    System.Diagnostics.Debug.WriteLine($">>> Latitude: {location.Latitude:F6}");
                    System.Diagnostics.Debug.WriteLine($">>> Longitude: {location.Longitude:F6}");
                    System.Diagnostics.Debug.WriteLine($">>> Accuracy: {location.Accuracy}m");
                    System.Diagnostics.Debug.WriteLine($">>> Altitude: {location.Altitude}m");
                    System.Diagnostics.Debug.WriteLine($">>> Timestamp: {location.Timestamp}");

#if ANDROID
                    if (location.IsFromMockProvider)
                    {
                        System.Diagnostics.Debug.WriteLine($">>> 📍 Location source: MOCK (from emulator/mock app)");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($">>> 📍 Location source: REAL GPS");
                    }
#endif

                    await Application.Current!.MainPage!.DisplayAlert(
                        "GPS Test Success",
                        $"Lat: {location.Latitude:F6}\nLon: {location.Longitude:F6}\nAccuracy: {location.Accuracy:F0}m",
                        "OK");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($">>> ❌ GPS TEST FAILED: Location is null");
                    await Application.Current!.MainPage!.DisplayAlert(
                        "GPS Test Failed",
                        "Unable to get location. Check permissions and GPS settings.",
                        "OK");
                }

                System.Diagnostics.Debug.WriteLine(">>> ===== GPS TEST ENDED =====\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> ❌ GPS test exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($">>> Stack trace: {ex.StackTrace}");

                await Application.Current!.MainPage!.DisplayAlert(
                    "GPS Test Error",
                    $"Error: {ex.Message}",
                    "OK");
            }
        }

        private async Task TestTts()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(">>> Testing TTS directly...");
                await TextToSpeech.Default.SpeakAsync("Xin chào, đây là test thuyết minh bằng Text to Speech");
                System.Diagnostics.Debug.WriteLine(">>> TTS test completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> TTS test failed: {ex.Message}");
            }
        }

        private async Task TestNarration()
        {
            if (_narrationService == null)
            {
                System.Diagnostics.Debug.WriteLine(">>> Narration service is null!");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine(">>> Testing Narration Service...");
                var testPoi = new POI
                {
                    Name = "Test POI",
                    TtsText = "Chào mừng bạn đến với quán ăn test. Đây là thử nghiệm hệ thống thuyết minh tự động.",
                    UseTts = true
                };
                await _narrationService.PlayNarrationAsync(testPoi);
                System.Diagnostics.Debug.WriteLine(">>> Narration test completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> Narration test failed: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            _updateFrequency = Preferences.Get(KEY_UPDATE_FREQUENCY, 5);
            _defaultRadius = Preferences.Get(KEY_DEFAULT_RADIUS, 50);
            _cooldownMinutes = Preferences.Get(KEY_COOLDOWN_MINUTES, 5);
            _preferTts = Preferences.Get(KEY_PREFER_TTS, false);
            _enableAudio = Preferences.Get(KEY_ENABLE_AUDIO, true);
            _enableBackgroundTracking = Preferences.Get(KEY_BACKGROUND_TRACKING, false); // Default: OFF

            // Load language preference
            var languageCode = Preferences.Get(KEY_APP_LANGUAGE, "vi");
            _selectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == languageCode) 
                               ?? AvailableLanguages.First();
        }

        private void StartBackgroundService()
        {
#if ANDROID
            var intent = new Android.Content.Intent(Android.App.Application.Context, 
                typeof(Platforms.Android.Services.LocationBackgroundService));
            Android.App.Application.Context.StartForegroundService(intent);
#endif
        }

        private void StopBackgroundService()
        {
#if ANDROID
            var intent = new Android.Content.Intent(Android.App.Application.Context, 
                typeof(Platforms.Android.Services.LocationBackgroundService));
            Android.App.Application.Context.StopService(intent);
#endif
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
