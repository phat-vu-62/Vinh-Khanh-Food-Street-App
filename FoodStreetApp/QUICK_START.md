# 🚀 Quick Start Guide - FoodStreetApp

## ⚡ 30-Second Setup

```bash
# 1. Restore packages
dotnet restore FoodStreetApp/FoodStreetApp.csproj

# 2. Build for Android
dotnet build FoodStreetApp/FoodStreetApp.csproj -f net10.0-android

# 3. Deploy to device/emulator
dotnet build -t:Run -f net10.0-android
```

---

## 📱 What You'll See

### On Launch:
1. **Database initializes** (auto-creates `foodstreet.db3`)
2. **3 sample POIs loaded** (Bánh Mì, Phở, Cơm Tấm)
3. **Map appears** with blue circles around POIs
4. **GPS starts tracking** your location

### When Near POI:
1. **Status shows**: "Walk {X}m to {POI Name}"
2. **Enter radius** (50m) → 🎵 Narration plays
3. **Status updates**: "🎵 Playing: {POI Name}"
4. **5-minute cooldown** prevents re-trigger

---

## 🎯 Key Files to Customize

### Add Your Own POIs
**File**: `FoodStreetApp/Data/POIRepository.cs`
**Method**: `SeedDataAsync()`

```csharp
new POI
{
    Name = "Your Vendor Name",
    Latitude = 10.7560,        // Your GPS coordinates
    Longitude = 106.7000,
    Radius = 50,               // Trigger distance (meters)
    Priority = 3,              // 1=Low, 2=Med, 3=High
    Description = "Short description",
    TtsText = "Narration text",
    UseTts = true,             // true=TTS, false=Audio
    IsActive = true
}
```

### Change Update Frequency
**File**: `FoodStreetApp/Services/LocationService.cs`
**Line**: 41

```csharp
await Task.Delay(5000, ...);  // 5000 = 5 seconds
```

### Change Cooldown Period
**File**: `FoodStreetApp/Models/POI.cs`
**Line**: 38

```csharp
public int CooldownSeconds { get; set; } = 300;  // 300 = 5 minutes
```

---

## 🎮 Testing Tips

### Without Moving:
1. **Large radius**: Set POI radius to 500m
2. **Emulator GPS**: Use emulator location simulator
3. **Reset button**: Click "🔄 Reset All" to clear cooldowns

### Test Priority:
```csharp
// Same location, different priorities
new POI { Name = "Low", Priority = 1, Latitude = 10.7560, ... }
new POI { Name = "High", Priority = 3, Latitude = 10.7560, ... }
```
→ High priority triggers first

### Test Cooldown:
1. Enter POI zone → Plays ✅
2. Exit & re-enter → Silent (cooldown) ⏱️
3. Wait 5 min OR Reset → Plays again ✅

---

## 🐛 Debug Output

Check **Visual Studio Output Window** for:

```
GeofenceService initialized with 3 POIs
--- Geofence Check: 10.755900, 106.699500 ---
POI: Bánh Mì | Distance: 12.45m | Priority: 3 | CooldownOK: True
>>> GEOFENCE TRIGGERED: Bánh Mì (Priority: 3)
>>> Playing TTS: Bánh Mì Huỳnh Hoa
```

---

## 📦 Project Structure

```
Models/        → POI data structure
Data/          → SQLite repository
Services/      → Business logic
  ├── Location    (GPS tracking)
  ├── Geofence    (Distance + priority)
  ├── Narration   (Audio + TTS)
  └── POI         (Cache management)
ViewModels/    → MVVM logic
Views/         → UI (Map + Status)
```

---

## ⚙️ Architecture Flow

```
User walks
  ↓
GPS updates (5s)
  ↓
GeofenceService checks all POIs
  ↓
Priority + Distance + Cooldown check
  ↓
NarrationService plays audio/TTS
  ↓
UI updates status
```

---

## 🎨 UI Features

### Top Bar:
- **🔄 Reset All** - Clear cooldowns

### Map:
- **Blue circles** - POI trigger zones
- **Pin markers** - Tap for details
- **Your location** - Blue dot

### Bottom Panel:
- **📍 GPS coords** - Lat/Long
- **🎯 Nearest POI** - Distance + Priority
- **Status** - Current action
- **App title** - Branding

---

## 🔐 Permissions

### Android (Auto-requested):
- ACCESS_COARSE_LOCATION ✅
- ACCESS_FINE_LOCATION ✅

### iOS (Auto-requested):
- NSLocationWhenInUseUsageDescription ✅

---

## 🆘 Common Issues

### "No GPS location"
→ Enable location services on device
→ Grant permissions when prompted

### "No narration plays"
→ Check POI has `TtsText` OR `AudioFile`
→ Check cooldown hasn't blocked re-trigger
→ Click "Reset All" button

### "Can't see POIs on map"
→ Check database initialized (see debug output)
→ Check POI coordinates are near your location
→ Increase POI radius for testing

---

## 📊 NuGet Packages Used

- `Microsoft.Maui.Controls` - Core framework
- `Microsoft.Maui.Controls.Maps` - Map display
- `Plugin.Maui.Audio` - Audio playback
- `sqlite-net-pcl` - SQLite database
- `SQLitePCLRaw.bundle_green` - SQLite support

---

## 🎓 Learn More

- **Full documentation**: `README.md`
- **Implementation details**: `IMPLEMENTATION_SUMMARY.md`
- **MAUI docs**: https://learn.microsoft.com/dotnet/maui/

---

## ✅ Quick Checklist

- [ ] Packages restored (`dotnet restore`)
- [ ] Location permissions granted
- [ ] GPS enabled on device
- [ ] POIs configured in `POIRepository.cs`
- [ ] Audio files in `Resources/Raw/` OR TTS enabled
- [ ] App built and deployed
- [ ] Map visible with markers
- [ ] Status panel showing location
- [ ] Walk near POI → Narration plays ✅

---

**You're ready to go!** 🎉

Walk near a POI and hear the narration automatically. Enjoy exploring Vinh Khanh Street! 🍜🗺️
