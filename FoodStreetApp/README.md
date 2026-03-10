# Food Street Audio Guide - Vinh Khanh Street

A .NET MAUI mobile app that automatically narrates information when users approach Points of Interest (POIs) on Vinh Khanh Street, District 4, Ho Chi Minh City.

## 🎯 Core Features

### 1. **GPS Tracking**
- Continuous foreground location tracking
- Updates every 5 seconds
- Accurate distance calculation using Haversine formula

### 2. **POI System**
Each POI contains:
- **Coordinates**: Latitude, Longitude, Radius
- **Priority**: Higher priority POIs trigger first
- **Content**: Title, Description
- **Narration**: Audio file OR Text-to-Speech
- **Cooldown**: Prevents duplicate triggers (default 5 minutes)

### 3. **Geofencing Engine**
- Detects when user enters POI radius
- Triggers highest priority POI within range
- Respects cooldown periods
- Prevents duplicate playback

### 4. **Narration Engine**
- **Audio playback** for pre-recorded files
- **Text-to-Speech (TTS)** for dynamic narration
- Automatic queue management
- Stop/start controls

### 5. **Map UI**
- Interactive map with user location
- POI markers with tap-to-view details
- Visual radius circles around POIs
- Real-time distance display
- Priority indicators

### 6. **Data Storage**
- **SQLite database** for offline POI storage
- Auto-initialization with seed data
- CRUD operations support

## 📁 Project Architecture

```
FoodStreetApp/
├── Models/
│   └── POI.cs                      # POI entity with SQLite attributes
│
├── Data/
│   ├── IPOIRepository.cs           # Repository interface
│   └── POIRepository.cs            # SQLite implementation with seed data
│
├── Services/
│   ├── ILocationService.cs         # Location tracking interface
│   ├── LocationService.cs          # GPS tracking implementation
│   │
│   ├── IGeofenceService.cs         # Geofence detection interface
│   ├── GeofenceService.cs          # Haversine + priority logic
│   │
│   ├── INarrationService.cs        # Narration interface
│   ├── NarrationService.cs         # Audio + TTS playback
│   │
│   ├── IAudioService.cs            # Audio playback interface
│   ├── AudioService.cs             # Plugin.Maui.Audio wrapper
│   │
│   ├── IPOIService.cs              # POI management interface
│   └── POIService.cs               # POI cache & status tracking
│
├── ViewModels/
│   └── MapPageViewModel.cs         # Map page logic with MVVM
│
└── Views/
    ├── MapPage.xaml                # Map UI with status panel
    └── MapPage.xaml.cs             # Map interactions & pin handling
```

## 🔧 Technology Stack

### NuGet Packages
- `Microsoft.Maui.Controls` - Core MAUI framework
- `Microsoft.Maui.Controls.Maps` - Map integration
- `Plugin.Maui.Audio` (v3.0.1) - Audio playback
- `sqlite-net-pcl` (v1.9.172) - SQLite ORM
- `SQLitePCLRaw.bundle_green` (v2.1.10) - SQLite native support

### Services (Dependency Injection)
All services are registered in `MauiProgram.cs`:
- **Singleton**: Repository, all services
- **Transient**: ViewModels, Pages

## 🚀 Setup Instructions

### 1. Restore NuGet Packages
```bash
dotnet restore
```

### 2. Database Auto-Initialization
The SQLite database is automatically created on first launch at:
```
{AppDataDirectory}/foodstreet.db3
```

Seed data includes 3 sample POIs on Vinh Khanh Street.

### 3. Add Audio Files (Optional)
Place MP3 files in `Resources/Raw/` folder:
- `audio_banhmi.mp3`
- `audio_pho.mp3`
- `audio_comtam.mp3`

Or enable TTS by setting `UseTts = true` in database.

### 4. Configure POIs
Edit `Data/POIRepository.cs` → `SeedDataAsync()` method:

```csharp
new POI
{
    Name = "Your Vendor Name",
    Latitude = 10.7559,           // GPS coordinates
    Longitude = 106.6995,
    Radius = 50,                  // Trigger radius in meters
    Priority = 3,                 // 1=Low, 2=Medium, 3=High
    Description = "Description",
    AudioFile = "audio_file.mp3", // OR use TTS
    TtsText = "TTS narration",    // Text for TTS
    UseTts = false,               // true = TTS, false = Audio
    CooldownSeconds = 300,        // 5 minutes cooldown
    IsActive = true
}
```

### 5. Platform Permissions

**Android** (`Platforms/Android/AndroidManifest.xml`):
```xml
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
```

**iOS** (`Platforms/iOS/Info.plist`):
```xml
<key>NSLocationWhenInUseUsageDescription</key>
<string>This app needs location for audio guides</string>
```

### 6. Build & Run
```bash
# Android
dotnet build -f net10.0-android

# iOS
dotnet build -f net10.0-ios
```

## 🎮 How It Works

### Startup Sequence
1. **App Launch** → Initialize SQLite database
2. **Load POIs** → Read active POIs from database
3. **Initialize Geofences** → Set up geofence monitoring
4. **Start GPS** → Begin location tracking (every 5 seconds)
5. **Display Map** → Show POIs with radius circles

### Runtime Flow
```
GPS Update (every 5s)
    ↓
Calculate distances to all POIs
    ↓
Find POI within radius + highest priority + cooldown expired
    ↓
Trigger Narration (Audio OR TTS)
    ↓
Update status display
    ↓
Set cooldown timer (prevents re-trigger for 5 minutes)
```

### Geofence Priority Logic
When multiple POIs are in range:
1. **Filter**: Active POIs within radius
2. **Check**: Cooldown period expired
3. **Sort**: By priority (3 > 2 > 1)
4. **Select**: Closest POI among highest priority
5. **Trigger**: Play narration

## 🎛️ User Interface

### Map Controls
- **🔄 Reset All** button - Clear all cooldowns and allow re-triggers
- **Blue circles** - Visual POI trigger zones
- **Pin markers** - Tap to view POI details

### Status Panel (Bottom)
- **📍 GPS coordinates** - Current latitude/longitude
- **🎯 Nearest POI** - Distance and priority
- **Status message** - Current action or instruction
- **App title** - Vinh Khanh Street Audio Guide

### Status Messages
- `✅ Inside {POI} zone` - Currently in trigger area
- `🎵 Playing: {POI}` - Narration is playing
- `Walk {distance}m to {POI}` - Navigation instruction
- `Exploring Vinh Khanh Street` - Default state

## 🛠️ Customization

### Change Location Update Frequency
`Services/LocationService.cs`, line 41:
```csharp
await Task.Delay(5000, _cancelTokenSource.Token);  // 5000ms = 5 seconds
```

### Change Default Cooldown Period
`Models/POI.cs`, line 38:
```csharp
public int CooldownSeconds { get; set; } = 300;  // 5 minutes
```

### Change Map Zoom Level
`Views/MapPage.xaml.cs`, line 25:
```csharp
map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(0.5)));
```

### Add New POI to Database
Use `IPOIRepository.SavePOIAsync()`:
```csharp
var newPoi = new POI
{
    Name = "New Vendor",
    Latitude = 10.7560,
    Longitude = 106.7000,
    Radius = 50,
    Priority = 2,
    TtsText = "Welcome to new vendor",
    UseTts = true,
    IsActive = true
};
await repository.SavePOIAsync(newPoi);
```

## 📊 Debug Logging

Enable debug output in Visual Studio Output window:

```
--- Geofence Check: 10.755900, 106.699500 ---
POI: Bánh Mì Huỳnh Hoa | Distance: 12.45m | Radius: 50m | Priority: 3 | CooldownOK: True
>>> GEOFENCE TRIGGERED: Bánh Mì Huỳnh Hoa (Priority: 3)
>>> Playing TTS: Bánh Mì Huỳnh Hoa
```

## 🧪 Testing Tips

### Test Without Physical Movement
1. Use emulator location simulation
2. Set large POI radius (e.g., 500m) for easier triggering
3. Use **Reset All** button to clear cooldowns during testing
4. Enable TTS instead of audio files for faster testing

### Test Priority System
Create POIs with same coordinates but different priorities:
```csharp
new POI { Name = "Low Priority", Priority = 1, ... }
new POI { Name = "High Priority", Priority = 3, ... }
```
The high priority POI should trigger first.

### Test Cooldown
1. Enter POI zone → narration plays
2. Exit and re-enter immediately → no narration (cooldown active)
3. Wait 5 minutes OR click "Reset All"
4. Re-enter → narration plays again

## 📝 Notes

- **Production Ready**: Add error handling, analytics, background location
- **Battery Optimization**: Adjust GPS frequency for battery life
- **Offline Support**: All POI data stored locally in SQLite
- **Audio Files**: Place in `Resources/Raw/`, or use TTS for dynamic content
- **Privacy**: Location used only for geofencing, not uploaded

## 📄 License

This is a Proof of Concept application for educational purposes.

## 🤝 Contributing

To add more POIs:
1. Update `Data/POIRepository.cs` → `SeedDataAsync()`
2. Or use the repository methods to insert POIs programmatically
3. Add corresponding audio files to `Resources/Raw/`

---

**Built with .NET MAUI 10.0** 🚀
