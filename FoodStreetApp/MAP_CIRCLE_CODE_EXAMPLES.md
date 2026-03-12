# 📝 Map Circle Duplication - Code Examples

## Complete Implementation Guide

This document provides copy-paste ready code examples for fixing map circle duplication in .NET MAUI apps.

---

## 1. MapPage.xaml.cs - Complete File

```csharp
using FoodStreetApp.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace FoodStreetApp.Views
{
    public partial class MapPage : ContentPage
    {
        private readonly MapPageViewModel _viewModel;
        
        // ✅ Key improvement: Store references to prevent duplicates
        private readonly Dictionary<int, Pin> _poiPins = new();
        private readonly Dictionary<int, Circle> _poiCircles = new();
        
        // ✅ Key improvement: Track initialization state
        private bool _isMapInitialized = false;

        public MapPage(MapPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
            
            // Subscribe to location changes
            _viewModel.LocationChanged += OnViewModelLocationChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // ✅ Key improvement: Initialize ONLY once
            if (!_isMapInitialized)
            {
                await _viewModel.InitializeAsync();
                InitializeMapElements(); // Called ONCE
                _isMapInitialized = true;
                
                System.Diagnostics.Debug.WriteLine("✅ Map initialized (one-time setup)");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("✅ Map already initialized, skipping");
            }

            // Center map on current location
            CenterMapOnCurrentLocation();
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Unsubscribe events to prevent memory leaks
            _viewModel.LocationChanged -= OnViewModelLocationChanged;
            
            await _viewModel.CleanupAsync();
        }

        /// <summary>
        /// Initialize POI pins and circles once
        /// ✅ This method is called ONLY ONCE at startup
        /// </summary>
        private void InitializeMapElements()
        {
            // Clear any existing elements (safety)
            map.Pins.Clear();
            map.MapElements.Clear();
            _poiPins.Clear();
            _poiCircles.Clear();

            System.Diagnostics.Debug.WriteLine($">>> Initializing map with {_viewModel.POIs.Count} POIs");

            // Create pins and circles for each POI
            foreach (var poi in _viewModel.POIs)
            {
                // Create pin (marker)
                var pin = new Pin
                {
                    Label = poi.Name,
                    Address = poi.Description,
                    Type = PinType.Place,
                    Location = new Location(poi.Latitude, poi.Longitude)
                };
                pin.MarkerClicked += OnPinMarkerClicked;

                // Create geofence circle with priority-based color
                var circle = new Circle
                {
                    Center = new Location(poi.Latitude, poi.Longitude),
                    Radius = new Distance(poi.ApproachRadius),
                    StrokeColor = GetColorForPriority(poi.Priority),
                    StrokeWidth = 2,
                    FillColor = GetColorForPriority(poi.Priority).WithAlpha(0.15f) // 15% transparent
                };

                // Add to map
                map.Pins.Add(pin);
                map.MapElements.Add(circle);

                // ✅ Store references in dictionaries (prevents duplicates)
                _poiPins[poi.Id] = pin;
                _poiCircles[poi.Id] = circle;

                System.Diagnostics.Debug.WriteLine(
                    $">>> Added POI: {poi.Name} (Priority: {poi.Priority}, Radius: {poi.ApproachRadius}m)");
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ Created {_poiCircles.Count} circles (will not be recreated)");
        }

        /// <summary>
        /// Get color based on POI priority
        /// Higher priority = more visible color
        /// </summary>
        private Color GetColorForPriority(int priority)
        {
            return priority switch
            {
                >= 9 => Colors.Red,      // Highest priority (must-see)
                >= 7 => Colors.Orange,   // High priority (recommended)
                >= 5 => Colors.Blue,     // Medium priority (worth visiting)
                >= 3 => Colors.Green,    // Low priority (optional)
                _ => Colors.Gray         // Lowest priority (background info)
            };
        }

        /// <summary>
        /// Handle location updates from ViewModel
        /// ✅ This does NOT recreate circles
        /// </summary>
        private void OnViewModelLocationChanged(object? sender, Location location)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Only update UI text, not map elements
                // User can manually recenter using the button
                System.Diagnostics.Debug.WriteLine($"📍 Location updated: {location.Latitude:F6}, {location.Longitude:F6}");
            });
        }

        /// <summary>
        /// Center map on current location
        /// </summary>
        private void CenterMapOnCurrentLocation()
        {
            if (_viewModel.CurrentLocation != null)
            {
                var location = new Location(
                    _viewModel.CurrentLocation.Latitude, 
                    _viewModel.CurrentLocation.Longitude);
                    
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

        /// <summary>
        /// Center map to current location (manual button)
        /// </summary>
        private void OnCenterToLocationClicked(object sender, EventArgs e)
        {
            if (_viewModel.CurrentLocation != null)
            {
                CenterMapOnCurrentLocation();
            }
            else
            {
                DisplayAlert("Error", "Current location not available", "OK");
            }
        }
    }
}
```

---

## 2. Key Pattern: Initialization Check

### Pattern:

```csharp
// Declare flag
private bool _isMapInitialized = false;

// Check before initializing
protected override async void OnAppearing()
{
    if (!_isMapInitialized)
    {
        // Initialize map elements
        InitializeMapElements();
        
        // Set flag to prevent re-initialization
        _isMapInitialized = true;
    }
    
    // Always safe to call
    CenterMapOnCurrentLocation();
}
```

### Why This Works:

```
First call to OnAppearing():
  _isMapInitialized = false
  → InitializeMapElements() called
  → Circles created
  → _isMapInitialized = true

Second call to OnAppearing():
  _isMapInitialized = true
  → Skip initialization
  → Circles already exist
  → No duplicates!
```

---

## 3. Key Pattern: Dictionary Storage

### Pattern:

```csharp
// Declare dictionaries
private readonly Dictionary<int, Pin> _poiPins = new();
private readonly Dictionary<int, Circle> _poiCircles = new();

// Store references
private void InitializeMapElements()
{
    foreach (var poi in POIs)
    {
        var circle = new Circle { ... };
        
        // Add to map
        map.MapElements.Add(circle);
        
        // ✅ Store reference (key = POI.Id, value = Circle object)
        _poiCircles[poi.Id] = circle;
    }
}

// Check if circle exists
if (_poiCircles.ContainsKey(poiId))
{
    // Circle already exists, reuse it
    var existingCircle = _poiCircles[poiId];
}
```

### Why Use Dictionary:

```csharp
// ✅ Fast lookup: O(1)
var circle = _poiCircles[5]; // Instant access to POI #5's circle

// ✅ Prevent duplicates
if (_poiCircles.ContainsKey(5))
{
    // Don't create another circle
}

// ✅ Easy update
_poiCircles[5] = newCircle; // Replace existing

// ❌ Without dictionary (slow)
var circle = map.MapElements.OfType<Circle>()
    .FirstOrDefault(c => c.Center == poiLocation); // O(n) search
```

---

## 4. Priority-Based Colors

### Implementation:

```csharp
private Color GetColorForPriority(int priority)
{
    return priority switch
    {
        >= 9 => Colors.Red,      // 10, 9
        >= 7 => Colors.Orange,   // 8, 7
        >= 5 => Colors.Blue,     // 6, 5
        >= 3 => Colors.Green,    // 4, 3
        _ => Colors.Gray         // 2, 1, 0
    };
}

// Usage:
var circle = new Circle
{
    StrokeColor = GetColorForPriority(poi.Priority), // Outline color
    FillColor = GetColorForPriority(poi.Priority).WithAlpha(0.15f) // Fill with transparency
};
```

### Example POI Priorities:

```csharp
// In POIRepository.cs
var pois = new List<POI>
{
    new POI { Name = "Ốc Phát", Priority = 10 },        // 🔴 Red
    new POI { Name = "SINZIEN", Priority = 9 },         // 🔴 Red
    new POI { Name = "Ốc Thảo", Priority = 8 },         // 🟠 Orange
    new POI { Name = "Ti Ti", Priority = 7 },           // 🟠 Orange
    new POI { Name = "Đầu Trọc", Priority = 6 },        // 🔵 Blue
    new POI { Name = "Hải Ký", Priority = 5 },          // 🔵 Blue
    new POI { Name = "Cơm Gà", Priority = 4 },          // 🟢 Green
    new POI { Name = "Ốc 35k", Priority = 3 },          // 🟢 Green
    new POI { Name = "Phố Hải Sản", Priority = 3 },     // 🟢 Green
    new POI { Name = "Toàn Phương", Priority = 3 },     // 🟢 Green
    new POI { Name = "Ốc Vũ", Priority = 2 },           // ⚪ Gray
    new POI { Name = "Ốc Loan", Priority = 2 }          // ⚪ Gray
};
```

---

## 5. Circle Styling

### Recommended Settings:

```csharp
var circle = new Circle
{
    // Location
    Center = new Location(poi.Latitude, poi.Longitude),
    
    // Size
    Radius = new Distance(poi.ApproachRadius), // e.g., 200 meters
    
    // Outline
    StrokeColor = Colors.Blue,  // Border color
    StrokeWidth = 2,            // 2-pixel border
    
    // Fill
    FillColor = Colors.Blue.WithAlpha(0.15f) // 15% opacity (very transparent)
};
```

### Alpha Values Comparison:

```csharp
// Too opaque (hard to see map)
FillColor = Colors.Blue.WithAlpha(1.0f)   // 100% ❌
FillColor = Colors.Blue.WithAlpha(0.5f)   // 50%  ❌
FillColor = Colors.Blue.WithAlpha(0.3f)   // 30%  ⚠️

// Good (can see map clearly)
FillColor = Colors.Blue.WithAlpha(0.2f)   // 20%  ✅
FillColor = Colors.Blue.WithAlpha(0.15f)  // 15%  ✅ Best!
FillColor = Colors.Blue.WithAlpha(0.1f)   // 10%  ✅
```

---

## 6. GPS Update Handling (Do NOT Recreate Circles)

### ❌ WRONG Way:

```csharp
// BAD: Creates circles on every GPS update
private void OnLocationChanged(Location location)
{
    // Update location text
    CurrentLocation = location;
    
    // ❌ DON'T DO THIS:
    map.MapElements.Clear(); // Removes all circles
    
    foreach (var poi in POIs)
    {
        var circle = new Circle { ... };
        map.MapElements.Add(circle); // Recreates circles
    }
}
```

### ✅ CORRECT Way:

```csharp
// GOOD: Only update text, not map elements
private void OnLocationChanged(Location location)
{
    // Update location property (text binding updates UI)
    CurrentLocation = location;
    
    // Update status text
    CurrentLocationText = $"📍 {location.Latitude:F6}, {location.Longitude:F6}";
    
    // Check geofences (no map rendering)
    var triggeredPoi = _geofenceService.CheckGeofences(location);
    
    // ✅ Circles remain unchanged
    // ✅ No map.MapElements.Add() calls
    // ✅ No duplicate circles
}
```

---

## 7. Complete Lifecycle

### Sequence Diagram:

```
App Start
   │
   ▼
MapPage Constructor
   ├─ Create dictionaries
   ├─ Subscribe to LocationChanged event
   └─ Set BindingContext
   │
   ▼
OnAppearing (1st time)
   ├─ _isMapInitialized = false
   ├─ Call InitializeAsync()
   ├─ Call InitializeMapElements()
   │  ├─ Create 12 pins
   │  ├─ Create 12 circles
   │  └─ Store in dictionaries
   ├─ Set _isMapInitialized = true
   └─ CenterMapOnCurrentLocation()
   │
   ▼
GPS Updates (every 5 seconds)
   ├─ OnLocationChanged() called
   ├─ Update CurrentLocation property
   ├─ Check geofences
   └─ No circle recreation ✅
   │
   ▼
Navigate Away
   ├─ OnDisappearing() called
   ├─ Unsubscribe events
   └─ Cleanup
   │
   ▼
Navigate Back
   ├─ OnAppearing (2nd time)
   ├─ _isMapInitialized = true
   ├─ Skip initialization ✅
   └─ CenterMapOnCurrentLocation()
```

---

## 8. Testing Checklist

### Verify the Fix:

```csharp
// Add debug logging to verify
private void InitializeMapElements()
{
    System.Diagnostics.Debug.WriteLine("🔵 InitializeMapElements() called");
    
    foreach (var poi in _viewModel.POIs)
    {
        // Create circle...
        System.Diagnostics.Debug.WriteLine($"  ✅ Created circle for {poi.Name}");
    }
    
    System.Diagnostics.Debug.WriteLine($"🔵 Total circles created: {_poiCircles.Count}");
}

protected override async void OnAppearing()
{
    if (!_isMapInitialized)
    {
        System.Diagnostics.Debug.WriteLine("🟢 First OnAppearing - will initialize");
        InitializeMapElements();
        _isMapInitialized = true;
    }
    else
    {
        System.Diagnostics.Debug.WriteLine("🟡 Subsequent OnAppearing - skipping initialization");
    }
}
```

### Expected Console Output:

```
First launch:
🟢 First OnAppearing - will initialize
🔵 InitializeMapElements() called
  ✅ Created circle for Ốc Phát
  ✅ Created circle for SINZIEN
  ✅ Created circle for Ốc Thảo
  ...
🔵 Total circles created: 12

Navigate away and back:
🟡 Subsequent OnAppearing - skipping initialization

GPS updates (should NOT see "InitializeMapElements"):
📍 GPS Update: 10.761817, 106.702175
--- Geofence Check ---
✅ No circle recreation
```

---

## 9. Performance Monitoring

### Add Performance Logging:

```csharp
private void InitializeMapElements()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // Clear and recreate elements
    map.Pins.Clear();
    map.MapElements.Clear();
    
    foreach (var poi in _viewModel.POIs)
    {
        // Create pin and circle...
    }
    
    stopwatch.Stop();
    System.Diagnostics.Debug.WriteLine($"⏱️ Map initialization took {stopwatch.ElapsedMilliseconds}ms");
}
```

### Expected Results:

```
First launch:
⏱️ Map initialization took 120ms ✅

Subsequent:
(No initialization, 0ms) ✅

Memory usage:
Startup: 25 MB
After 5 minutes: 25 MB (stable) ✅
```

---

## 10. Summary

### Key Improvements:

1. **One-time initialization** (`_isMapInitialized` flag)
2. **Dictionary storage** (`_poiPins`, `_poiCircles`)
3. **Separated concerns** (init vs. GPS updates)
4. **Priority-based colors** (visual hierarchy)
5. **Transparent fills** (15% alpha)

### Before vs. After:

```csharp
// BEFORE: ❌
protected override async void OnAppearing()
{
    foreach (var poi in POIs)
    {
        var circle = new Circle { ... };
        map.MapElements.Add(circle); // Duplicate every time!
    }
}

// AFTER: ✅
private bool _isMapInitialized = false;
private Dictionary<int, Circle> _poiCircles = new();

protected override async void OnAppearing()
{
    if (!_isMapInitialized)
    {
        InitializeMapElements(); // Once only!
        _isMapInitialized = true;
    }
}
```

### Result:

- ✅ No circle duplication
- ✅ Stable memory usage
- ✅ Smooth map rendering
- ✅ Clear visual hierarchy
- ✅ Production-ready code

🎉 **Problem solved!**
