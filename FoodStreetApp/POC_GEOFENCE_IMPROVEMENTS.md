# POC Geofence & Narration Improvements

## Root Cause Summary

| Problem | Root Cause | Fix Applied |
|---------|-----------|-------------|
| Same POI always triggers first | `ApproachRadius=200m` used as trigger — ALL 12 POIs are in-range simultaneously | Use `poi.Radius + 10m` (≈60m) as trigger zone |
| POIs skip/jump during narration | 12 POIs within 5–15m of each other all compete | Clustering filter removes POIs within 40m of a higher-priority POI |
| GPS feels slow / POI missed | 5-second polling interval | Polling reduced to 2 seconds |
| Map circles are huge / cluttered | Circles drawn at `ApproachRadius=200m` | Circles now drawn at `poi.Radius=50m` |
| Same POI narrates repeatedly | No global narration gap | 15-second global gap between any two narrations |

---

## Changes Made

### 1. `GeofenceService.cs` — Complete rewrite

**Clustering filter** (`ApplyClusteringFilter`):
- Processes POIs sorted by priority DESC
- If a candidate is within `MinimumPOISpacingMeters` (40m) of an already-kept POI, it is dropped
- With the seeded data (12 POIs on ~100m of street), this results in ~2–3 active POIs

```
[CLUSTER] Kept:     'Ốc Phát' (Priority 10)
[CLUSTER] Filtered: 'Quán Nước SINZIEN' (Priority 9)  — 9.5m from Ốc Phát
[CLUSTER] Filtered: 'Nướng Ngói Ti Ti' (Priority 7)   — 11m from Ốc Phát
[CLUSTER] Filtered: 'Đầu Trọc Tiệm Nướng' (Priority 6) — 20m from Ốc Phát
[CLUSTER] Filtered: 'Hải Ký Mì Gia' (Priority 5)      — 38m from Ốc Phát
[CLUSTER] Kept:     'Cơm Gà Như Thủy' (Priority 4)    — 48m from Ốc Phát
...
```

**Trigger radius** now uses `poi.Radius + TriggerBufferMeters` (50 + 10 = 60m) — NOT the 200m `ApproachRadius`.

**Global narration gap** (15 seconds): After any narration fires, the engine is silent for 15 seconds.

**Per-POI cooldown**: Unchanged — uses `poi.CooldownSeconds` (60s in seed data).

**Last-narrated-POI guard**: If the best candidate is the same POI that just played, it is skipped until its per-POI cooldown expires.

**Selection algorithm**:
```
candidates
  .Where(distance <= poi.Radius + 10m AND poiCooldownExpired)
  .OrderBy(distance)           // closest first
  .ThenByDescending(priority)  // priority as tiebreaker
  .First()
```

**New interface method**: `GetClusteredPOIs()` — returns the POIs that survived clustering, used by the map for display.

---

### 2. `IGeofenceService.cs`
Added `List<POI> GetClusteredPOIs()` to the interface.

---

### 3. `LocationService.cs`
GPS polling interval changed: **5000ms → 2000ms** (both normal loop and error-retry).

---

### 4. `MapPage.xaml.cs`
Map circles now use **`poi.Radius` (50m)** instead of `poi.ApproachRadius` (200m).  
Before: 200m-radius overlapping blobs covering the entire block.  
After: Clean 50m circles, one per POI, clearly showing individual restaurants.

---

### 5. `MapPageViewModel.cs`
After `GeofenceService.InitializeAsync()`, the map's POI collection is **replaced with the clustered subset**:
```csharp
var clusteredPois = _geofenceService.GetClusteredPOIs();
POIs.Clear();
foreach (var poi in clusteredPois) POIs.Add(poi);
```
This means only non-overlapping POIs appear as pins + circles on the map.

---

## Tunable Constants (in `GeofenceService.cs`)

```csharp
private const double TriggerBufferMeters       = 10.0;  // extra meters added to poi.Radius
private const double MinimumPOISpacingMeters   = 40.0;  // clustering minimum distance
private const double MinimumNarrationGapSeconds = 15.0;  // global gap between narrations
```

Increase `MinimumPOISpacingMeters` → fewer active POIs (less narration).  
Decrease `MinimumPOISpacingMeters` → more active POIs (more narration, possible overlap).  
Increase `MinimumNarrationGapSeconds` → more breathing room between narrations.

---

## Debug Log Prefixes

| Prefix | Meaning |
|--------|---------|
| `[GPS]` | Location update received |
| `[CLUSTER]` | Clustering filter decision (kept / filtered) |
| `[GEOFENCE]` | Initialization summary or global gap |
| `[POI]` | Per-POI distance check result |
| `[CANDIDATE]` | POI added to candidate list |
| `[SKIP]` | POI excluded (outside radius / cooldown / same as last) |
| `[TRIGGER]` | Narration fired |
| `[MAP]` | Map pin/circle added |

---

## Build Status: ✅ Successful
