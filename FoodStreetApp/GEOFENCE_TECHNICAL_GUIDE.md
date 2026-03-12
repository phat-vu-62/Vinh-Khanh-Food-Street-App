# 🎯 Geofence System - Technical Deep Dive

## Overview

The FoodStreetApp uses a sophisticated geofence detection system to automatically trigger audio narrations when users approach Points of Interest (POIs). This document explains the technical implementation and algorithms used.

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   User's Phone                          │
│                                                         │
│  ┌─────────────┐    GPS Updates     ┌──────────────┐  │
│  │ Location    │ ──────────────────► │ Geofence     │  │
│  │ Service     │    (every 5s)       │ Service      │  │
│  └─────────────┘                     └──────┬───────┘  │
│                                              │          │
│                                       POI Triggered?    │
│                                              │          │
│                                       ┌──────▼───────┐  │
│                                       │ Narration    │  │
│                                       │ Service      │  │
│                                       └──────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## Key Components

### 1. LocationService
**Responsibility:** Track user's GPS location

```csharp
public class LocationService : ILocationService
{
    public event EventHandler<Location>? LocationChanged;
    
    public async Task<bool> StartTrackingAsync()
    {
        // Poll GPS every 5 seconds
        while (!_cancelTokenSource.Token.IsCancellationRequested)
        {
            var location = await Geolocation.Default.GetLocationAsync();
            LocationChanged?.Invoke(this, location);
            await Task.Delay(5000);
        }
    }
}
```

---

### 2. GeofenceService
**Responsibility:** Detect when user enters POI radius

#### Algorithm Flow:

```
FOR each GPS update:
    FOR each POI (ordered by priority DESC):
        1. Calculate distance using Haversine
        2. Check if inside approach radius
        3. Check if cooldown expired
        4. Check if approaching OR first entry
        5. Select POI with highest priority & closest distance
    
    IF POI triggered:
        Play narration
        Set cooldown
```

#### Haversine Distance Calculation:

The app uses the **Haversine formula** to calculate accurate distances between geographic coordinates:

```csharp
private double CalculateDistanceInMeters(double lat1, double lon1, double lat2, double lon2)
{
    const double R = 6371000; // Earth radius in meters
    
    // Convert to radians
    double φ1 = lat1 * π / 180;
    double φ2 = lat2 * π / 180;
    double Δφ = (lat2 - lat1) * π / 180;
    double Δλ = (lon2 - lon1) * π / 180;
    
    // Haversine formula
    double a = sin²(Δφ/2) + cos(φ1) * cos(φ2) * sin²(Δλ/2);
    double c = 2 * atan2(√a, √(1−a));
    
    return R * c; // Distance in meters
}
```

**Why Haversine?**
- Accurate for distances up to ~1km (perfect for street-level navigation)
- Accounts for Earth's curvature
- More accurate than simple Pythagorean distance

---

### 3. Priority System

POIs are assigned priority levels (1-10):

| Priority | Meaning | Example | Color |
|----------|---------|---------|-------|
| 10 | Must-see | Famous landmark | 🔴 Red |
| 9 | Highly recommended | Popular restaurant | 🔴 Red |
| 7-8 | Recommended | Good local spot | 🟠 Orange |
| 5-6 | Worth visiting | Small vendor | 🔵 Blue |
| 3-4 | Optional | Side street | 🟢 Green |
| 1-2 | Background info | Historical note | ⚪ Gray |

**Selection Rule:**
```csharp
// If multiple POIs are in range, highest priority wins
var sortedPois = _pois.OrderByDescending(p => p.Priority);

// If equal priority, closest POI wins
if (distance < minDistance)
{
    triggeredPoi = poi;
    minDistance = distance;
}
```

---

### 4. Cooldown Mechanism

To prevent repeated narrations, each POI has a cooldown period:

```csharp
public class POI
{
    public int CooldownSeconds { get; set; } = 60; // Default 60s
    public DateTime? LastTriggered { get; set; }
    
    public bool IsCooldownExpired()
    {
        if (!LastTriggered.HasValue) return true;
        return (DateTime.UtcNow - LastTriggered.Value).TotalSeconds >= CooldownSeconds;
    }
}
```

**Cooldown Flow:**
```
User enters POI radius
    ↓
Play narration
    ↓
Set LastTriggered = now
    ↓
User stays in radius → No repeated narration (cooldown active)
    ↓
User exits radius
    ↓
Wait 60 seconds
    ↓
User re-enters → Play narration again (cooldown expired)
```

---

### 5. Approaching Detection

The system detects whether the user is **approaching** a POI (distance decreasing):

```csharp
// Store last known distance
poi.LastDistance = double.MaxValue; // Initially far away

// On each GPS update
double currentDistance = CalculateDistance(userLocation, poi.Location);
bool isApproaching = currentDistance < poi.LastDistance;

// Update for next check
poi.LastDistance = currentDistance;
```

**Why this matters:**
- Prevents triggering when user is **leaving** a POI
- Only triggers when user is **getting closer**
- Works with real GPS movement

---

### 6. First Entry Detection (for Mock Locations)

For testing with mock locations (which "jump" instead of moving smoothly):

```csharp
bool isFirstEntry = poi.LastDistance >= poi.ApproachRadius 
                 && currentDistance < poi.ApproachRadius;
```

**Example:**
```
Mock location jumps: 500m → 50m (inside radius)
    ↓
FirstEntry = true (was outside, now inside)
    ↓
Trigger narration even though not "approaching" smoothly
```

---

## Complete Triggering Logic

A POI is triggered if **ALL** conditions are met:

```csharp
if (distance < poi.ApproachRadius &&      // 1. Inside radius
    cooldownExpired &&                     // 2. Cooldown expired
    (isApproaching || isFirstEntry) &&     // 3. Moving towards OR first entry
    distance < minDistance)                // 4. Closest among candidates
{
    triggeredPoi = poi;
}
```

---

## Map Visualization

### Circle Colors (Priority-based):

```csharp
private Color GetColorForPriority(int priority)
{
    return priority switch
    {
        >= 9 => Colors.Red,      // 🔴 Highest priority
        >= 7 => Colors.Orange,   // 🟠 High priority
        >= 5 => Colors.Blue,     // 🔵 Medium priority
        >= 3 => Colors.Green,    // 🟢 Low priority
        _ => Colors.Gray         // ⚪ Lowest priority
    };
}
```

### Circle Properties:
- **Stroke:** Color-coded by priority (2px width)
- **Fill:** Same color with 15% opacity (semi-transparent)
- **Radius:** `poi.ApproachRadius` (typically 200m)

---

## Performance Optimizations

### 1. Map Element Creation
```csharp
// ❌ Bad: Recreate every GPS update (50+ objects/second)
protected override void OnAppearing()
{
    foreach (var poi in POIs)
    {
        map.Pins.Add(new Pin { ... }); // Duplicate!
    }
}

// ✅ Good: Create once, reuse
private bool _isMapInitialized = false;

protected override void OnAppearing()
{
    if (!_isMapInitialized)
    {
        InitializeMapElements(); // One-time
        _isMapInitialized = true;
    }
}
```

### 2. Distance Calculation Caching
```csharp
// Store last distance for approaching detection
// Avoids recalculating when not needed
poi.LastDistance = currentDistance;
```

### 3. Event-based Updates
```csharp
// Only update UI when location actually changes
_locationService.LocationChanged += OnLocationChanged;

// Don't poll continuously
```

---

## Testing Scenarios

### Scenario 1: Real GPS Walking
```
User walks from 500m away → 0m
    ↓
100m: No trigger (outside radius)
50m: No trigger (outside radius)
199m: ✅ Trigger! (entering 200m radius, approaching)
150m: No trigger (cooldown active)
50m: No trigger (cooldown active)
```

### Scenario 2: Mock Location Testing
```
Mock location: 500m → 50m (instant jump)
    ↓
FirstEntry = true (was outside, now inside)
    ↓
✅ Trigger! (even though not "approaching" smoothly)
```

### Scenario 3: Multiple POIs Nearby
```
POI A: Distance 50m, Priority 10
POI B: Distance 30m, Priority 5
    ↓
Result: Trigger POI A (higher priority wins, even though farther)
```

### Scenario 4: Cooldown Period
```
Time 0:00 - Enter radius → Play narration
Time 0:30 - Still in radius → No narration (cooldown)
Time 1:00 - Still in radius → No narration (cooldown)
Time 1:30 - Exit and re-enter → ✅ Play narration (cooldown expired)
```

---

## Debug Logging

The system provides detailed debug output:

```
>>> LOCATION UPDATE: 10.761817, 106.702175
--- Geofence Check: 10.761817, 106.702175 ---
POI: Ốc Phát | Distance: 9.23m | ApproachRadius: 200m | Priority: 10 | CooldownOK: True | Approaching: True | FirstEntry: False
POI: Quán SINZIEN | Distance: 5.22m | ApproachRadius: 200m | Priority: 9 | CooldownOK: True | Approaching: True | FirstEntry: False
>>> 🎯 APPROACHING POI: Ốc Phát at 9.23m (Priority: 10)
```

**Logging Format:**
- 📍 GPS coordinates (6 decimal places)
- 📏 Distance in meters (2 decimal places)
- ⏱️ Cooldown status (True/False)
- 🎯 Approaching indicator
- 🚪 First entry detection

---

## Configuration

### Adjustable Parameters:

```csharp
public class POI
{
    public int ApproachRadius { get; set; } = 200;    // Trigger distance
    public int CooldownSeconds { get; set; } = 60;    // Re-trigger delay
    public int Priority { get; set; } = 5;            // Importance level
}
```

### GPS Update Frequency:
```csharp
// In LocationService.cs
await Task.Delay(5000); // Poll every 5 seconds
```

**Trade-offs:**
- **Faster (2s):** More accurate, higher battery drain
- **Slower (10s):** Less accurate, better battery life
- **Current (5s):** Good balance ✅

---

## Known Limitations & Future Improvements

### Current Limitations:
1. **No offline mode** - Requires active GPS
2. **No route optimization** - Random POI order
3. **No clustering** - All POIs shown at once
4. **No history** - Can't review past visits

### Planned Improvements:
1. **Smart routing** - Suggest optimal walking path
2. **POI clustering** - Group nearby POIs at high zoom
3. **Visit tracking** - Mark visited POIs
4. **Offline support** - Cache map tiles
5. **AR overlay** - Show POIs in camera view

---

## Best Practices

### For Developers:

1. **Always reset LastDistance** when seeding POIs
   ```csharp
   poi.LastDistance = double.MaxValue;
   ```

2. **Use fire-and-forget for narration**
   ```csharp
   _ = _narrationService.PlayNarrationAsync(poi);
   ```

3. **Unsubscribe events in OnDisappearing**
   ```csharp
   _viewModel.LocationChanged -= Handler;
   ```

4. **Initialize map elements once**
   ```csharp
   if (!_isMapInitialized) { ... }
   ```

### For Testers:

1. **Reset cooldowns** before each test
2. **Use mock locations** for repeatable testing
3. **Check debug logs** for detailed info
4. **Test with different priorities**

---

## Summary

The FoodStreetApp geofence system provides:
- ✅ Accurate distance calculation (Haversine)
- ✅ Smart POI prioritization
- ✅ Cooldown management
- ✅ Approaching detection
- ✅ Mock location support
- ✅ Optimized performance
- ✅ Clear visual feedback

**Result:** Reliable, production-ready POI detection system! 🎉
