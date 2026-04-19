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
        private const string KEY_APP_UI_LANGUAGE = "app_ui_language";

        private readonly INarrationService? _narrationService;

        public event PropertyChangedEventHandler? PropertyChanged;


        public ObservableCollection<LanguageOption> AvailableLanguages { get; }

        private LanguageOption? _selectedLanguage;
        public LanguageOption? SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage == value) return;

                _selectedLanguage = value;
                if (value != null)
                {
                    Preferences.Set(KEY_APP_LANGUAGE, value.Code);
                    (_narrationService as NarrationService)?.SetLanguage(value.Code);
                    System.Diagnostics.Debug.WriteLine($"Narration Language changed to: {value.Name} ({value.Code})");
                }
                OnPropertyChanged();
            }
        }

        private LanguageOption? _selectedAppLanguage;
        public LanguageOption? SelectedAppLanguage
        {
            get => _selectedAppLanguage;
            set
            {
                if (_selectedAppLanguage == value) return;

                // Only act if there's a real change avoiding initialization trigger
                bool isInitialization = _selectedAppLanguage == null;
                _selectedAppLanguage = value;
                if (value != null)
                {
                    Preferences.Set(KEY_APP_UI_LANGUAGE, value.Code);
                    System.Diagnostics.Debug.WriteLine($"App UI Language changed to: {value.Name} ({value.Code})");

                    if (!isInitialization)
                    {
                        var culture = new System.Globalization.CultureInfo(value.Code);
                        LocalizationResourceManager.Instance.SetCulture(culture);
                    }
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
                if (_updateFrequency == value) return;
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
                if (_defaultRadius == value) return;
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
                if (_cooldownMinutes == value) return;
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
                if (_preferTts == value) return;
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
                if (_enableAudio == value) return;
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
                if (_enableBackgroundTracking == value) return;
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

        private void LoadSettings()
        {
            _updateFrequency = Preferences.Get(KEY_UPDATE_FREQUENCY, 5);
            _defaultRadius = Preferences.Get(KEY_DEFAULT_RADIUS, 15);
            _cooldownMinutes = Preferences.Get(KEY_COOLDOWN_MINUTES, 5);
            _preferTts = Preferences.Get(KEY_PREFER_TTS, false);
            _enableAudio = Preferences.Get(KEY_ENABLE_AUDIO, true);
            _enableBackgroundTracking = Preferences.Get(KEY_BACKGROUND_TRACKING, false); // Default: OFF

            // Load language preference
            var languageCode = Preferences.Get(KEY_APP_LANGUAGE, "vi");
            _selectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == languageCode) 
                               ?? AvailableLanguages.First();

            var appUiLanguageCode = Preferences.Get(KEY_APP_UI_LANGUAGE, "vi");
            _selectedAppLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == appUiLanguageCode) 
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
