namespace FoodStreetApp.Resources
{
    /// <summary>
    /// Centralized string resources for the Food Street Guide app
    /// All user-facing text should use these constants for consistency and easy localization
    /// </summary>
    public static class AppStrings
    {
        // App Title
        public const string AppTitle = "Food Street Guide";
        public const string AppSubtitle = "Vinh Khanh Street Audio Guide";

        // Tab Titles
        public const string TabMap = "Map";
        public const string TabPOIs = "POIs";
        public const string TabSettings = "Settings";

        // Map Page
        public const string MapTitle = "Food Street Guide";
        public const string ButtonReset = "Reset All";
        public const string ButtonCenter = "Center Location";
        public const string CurrentLocation = "Current Location";
        public const string NearestPOI = "Nearest POI";
        public const string Distance = "Distance";
        public const string NoLocation = "Location not available";
        public const string InsideZone = "Inside {0} zone";
        public const string WalkTo = "Walk {0}m to {1}";
        public const string Exploring = "Exploring Vinh Khanh Street";

        // POI List Page
        public const string POIListTitle = "Points of Interest";
        public const string POIListSubtitle = "All Restaurants & Locations";
        public const string ButtonPlayPreview = "Preview";
        public const string Priority = "Priority";
        public const string Radius = "Radius";
        public const string Meters = "meters";
        public const string Away = "away";

        // Settings Page
        public const string SettingsTitle = "Settings";
        
        // GPS & Location Section
        public const string SectionGPS = "GPS & Location";
        public const string UpdateFrequency = "Update frequency (seconds)";
        public const string UpdateFrequencyValue = "{0} seconds";
        
        // Geofence Section
        public const string SectionGeofence = "Geofence";
        public const string DefaultRadius = "Default trigger radius (meters)";
        public const string DefaultRadiusValue = "{0} meters";
        public const string CooldownPeriod = "Cooldown period (minutes)";
        public const string CooldownPeriodValue = "{0} minutes";
        
        // Audio & TTS Section
        public const string SectionAudio = "Audio & TTS";
        public const string NarrationLanguage = "Narration Language";
        public const string SelectLanguage = "Select Language";
        public const string PreferTTS = "Prefer TTS over audio files";
        public const string EnableAudio = "Enable audio notifications";
        
        // Testing Section
        public const string SectionTesting = "System Tests";
        public const string ButtonTestGPS = "Test GPS Location";
        public const string ButtonTestTTS = "Test TTS";
        public const string ButtonTestNarration = "Test Narration Service";
        public const string TestGPSHint = "Press GPS test to check your current location";
        
        // Background Section
        public const string SectionBackground = "Background Tracking";
        public const string EnableBackgroundTracking = "Track even when app is minimized";
        
        // About Section
        public const string SectionAbout = "About";
        public const string AboutTitle = "Vinh Khanh Food Street Guide";
        public const string Version = "Version 2.0.0";
        public const string BuiltWith = "Built with .NET MAUI";

        // Languages
        public const string LanguageVietnamese = "Vietnamese";
        public const string LanguageEnglish = "English";
        public const string LanguageKorean = "Korean";
        public const string LanguageChinese = "Chinese";
        public const string LanguageJapanese = "Japanese";

        // Alerts & Messages
        public const string AlertSuccess = "Success";
        public const string AlertError = "Error";
        public const string AlertInfo = "Info";
        public const string ButtonOK = "OK";
        public const string ButtonCancel = "Cancel";
        
        public const string ResetSuccess = "All POI cooldowns have been reset";
        public const string GPSTestSuccess = "GPS Location: {0}, {1}\nAccuracy: {2}m";
        public const string GPSTestFailed = "Unable to get location. Check permissions and GPS settings.";
        public const string PermissionsRequired = "Location permissions required. Please grant permissions in Settings.";

        // Status Messages
        public const string StatusInitializing = "Initializing...";
        public const string StatusLoadingPOIs = "Loading POIs from database...";
        public const string StatusGettingLocation = "Getting GPS location...";
        public const string StatusTracking = "Tracking location...";
        public const string StatusPlayingNarration = "Playing narration: {0}";
        public const string StatusCheckingPermissions = "Checking permissions...";
    }
}
