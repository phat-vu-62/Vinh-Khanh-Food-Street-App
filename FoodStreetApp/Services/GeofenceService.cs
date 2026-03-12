using FoodStreetApp.Models;

namespace FoodStreetApp.Services
{
    /// <summary>
    /// POC geofence service for Vinh Khanh street:
    ///   - ALL active POIs are checked on every GPS tick (no clustering for narration)
    ///   - Nearest POI within <see cref="TriggerRadiusMeters"/> wins; priority as tiebreaker
    ///   - Per-POI cooldown prevents re-triggering the same restaurant too quickly
    ///   - Global narration gap prevents back-to-back narrations from adjacent POIs
    ///   - "Last narrated" guard resets when the user walks outside every geofence
    ///   - Clustering filter is used ONLY for map-display via <see cref="GetClusteredPOIs"/>
    /// </summary>
    public class GeofenceService : IGeofenceService
    {
        private List<POI> _allPois = new();
        private List<POI> _clusteredPois = new();
        private readonly INarrationService _narrationService;

        // --- Narration flow state ---
        private POI? _lastNarratedPoi;
        private DateTime? _lastNarrationTime;

        // --- POC configuration constants ---

        /// <summary>Fixed trigger radius in meters — narration fires when the user is within this distance of a POI.</summary>
        private const double TriggerRadiusMeters = 50.0;

        /// <summary>
        /// Minimum spacing between retained POIs after clustering (meters).
        /// Two POIs closer than this keep only the higher-priority one.
        /// </summary>
        private const double MinimumPOISpacingMeters = 30.0;

        /// <summary>Minimum seconds that must pass between any two narrations (global gap).</summary>
        private const double MinimumNarrationGapSeconds = 15.0;

        public GeofenceService(INarrationService narrationService)
        {
            _narrationService = narrationService;
        }

        /// <summary>
        /// Load POIs and apply the clustering filter.
        /// Only clustered POIs will be checked during geofence evaluation.
        /// </summary>
        public Task InitializeAsync(List<POI> pois)
        {
            _allPois = pois;
            _clusteredPois = ApplyClusteringFilter(pois);

            System.Diagnostics.Debug.WriteLine(
                $"[GEOFENCE] Initialized: {_allPois.Count} POIs for narration | {_clusteredPois.Count} clustered for map display (min spacing: {MinimumPOISpacingMeters}m)");

            foreach (var p in _clusteredPois)
                System.Diagnostics.Debug.WriteLine(
                    $"[GEOFENCE]   Active: '{p.Name}' (Priority: {p.Priority}, TriggerRadius: {TriggerRadiusMeters:F0}m)");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the POIs that survived the clustering filter.
        /// Use this for map display to reduce visual clutter.
        /// </summary>
        public List<POI> GetClusteredPOIs() => _clusteredPois;

        /// <summary>
        /// Returns the <paramref name="count"/> nearest active POIs to <paramref name="location"/>,
        /// sorted by ascending distance. Uses all loaded POIs (not just the clustered subset)
        /// so the map always shows what is physically closest to the user.
        /// </summary>
        public List<(POI poi, double distance)> GetNearbyPOIs(Location location, int count)
        {
            return _allPois
                .Where(p => p.IsActive)
                .Select(p => (
                    poi: p,
                    distance: CalculateDistanceInMeters(
                        location.Latitude, location.Longitude,
                        p.Latitude, p.Longitude)))
                .OrderBy(x => x.distance)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Evaluate the current GPS location against ALL active POIs.
        ///
        /// Trigger order:
        ///   1. Skip if global narration gap has not elapsed.
        ///   2. Calculate distance to every active POI; build candidates within 70 m with expired cooldown.
        ///      When zero candidates: reset the "last narrated" guard so re-entry triggers again.
        ///   3. Sort candidates: nearest first, priority as tiebreaker.
        ///   4. Skip if the best candidate is the same POI narrated last (prevents instant repeat).
        ///   5. Trigger narration for the winning POI.
        /// </summary>
        public POI? CheckGeofences(Location currentLocation)
        {
            var now = DateTime.UtcNow;

            // --- 1. Global narration gap ---
            if (_lastNarrationTime.HasValue)
            {
                var elapsed = (now - _lastNarrationTime.Value).TotalSeconds;
                if (elapsed < MinimumNarrationGapSeconds)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[GEOFENCE] Global gap: {MinimumNarrationGapSeconds - elapsed:F1}s remaining (last: '{_lastNarratedPoi?.Name}')");
                    return null;
                }
            }

            // --- 2. Distance check for ALL active POIs ---
            var activePois = _allPois.Where(p => p.IsActive).ToList();
            var candidates = new List<(POI poi, double distance)>();

            System.Diagnostics.Debug.WriteLine($"[GEOFENCE] Checking {activePois.Count} POIs (trigger radius {TriggerRadiusMeters:F0}m):");
            foreach (var poi in activePois)
            {
                var distance = CalculateDistanceInMeters(
                    currentLocation.Latitude, currentLocation.Longitude,
                    poi.Latitude, poi.Longitude);

                var cooldownOk = !poi.LastTriggered.HasValue ||
                    (now - poi.LastTriggered.Value).TotalSeconds >= poi.CooldownSeconds;

                poi.LastDistance = distance;

                var inRange = distance <= TriggerRadiusMeters;
                var tag = inRange && cooldownOk ? "✅ IN " : inRange ? "⏱ CD " : "📏 OUT";
                System.Diagnostics.Debug.WriteLine(
                    $"  [{tag}] '{poi.Name}' {distance:F0}m | P{poi.Priority}" +
                    (!cooldownOk
                        ? $" | cd {poi.CooldownSeconds - (now - poi.LastTriggered!.Value).TotalSeconds:F0}s left"
                        : ""));

                if (inRange && cooldownOk)
                    candidates.Add((poi, distance));
            }

            // No POIs in range — clear the last-narrated guard so re-entry triggers again
            if (candidates.Count == 0)
            {
                if (_lastNarratedPoi != null)
                {
                    System.Diagnostics.Debug.WriteLine("[GEOFENCE] Outside all geofences — last-narrated guard cleared");
                    _lastNarratedPoi = null;
                }
                System.Diagnostics.Debug.WriteLine("[GEOFENCE] No candidates in range");
                return null;
            }

            // --- 3. Nearest POI wins; priority as tiebreaker ---
            var best = candidates
                .OrderBy(c => c.distance)
                .ThenByDescending(c => c.poi.Priority)
                .First();

            // --- 4. Same-POI guard (prevents repeating without leaving range) ---
            if (best.poi == _lastNarratedPoi)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[GEOFENCE] '{best.poi.Name}' still last-narrated — walk away to reset");
                return null;
            }

            // --- 5. Trigger ---
            System.Diagnostics.Debug.WriteLine(
                $"[TRIGGER] ▶ '{best.poi.Name}' at {best.distance:F0}m (Priority: {best.poi.Priority})");

            best.poi.LastTriggered = now;
            best.poi.HasPlayed = true;
            _lastNarratedPoi = best.poi;
            _lastNarrationTime = now;

            _ = _narrationService.PlayNarrationAsync(best.poi);
            return best.poi;
        }

            /// <summary>
            /// Reset all cooldowns and narration state (for testing / demo reset).
            /// </summary>
            public void ResetAllGeofences()
        {
            foreach (var poi in _allPois)
            {
                poi.HasPlayed = false;
                poi.LastTriggered = null;
                poi.LastDistance = double.MaxValue;
            }
            _lastNarratedPoi = null;
            _lastNarrationTime = null;
            System.Diagnostics.Debug.WriteLine("[GEOFENCE] All geofences and narration state reset");
        }

        /// <summary>
        /// Reset cooldown for a single POI by ID.
        /// </summary>
        public void ResetGeofence(int poiId)
        {
            var poi = _allPois.FirstOrDefault(p => p.Id == poiId);
            if (poi != null)
            {
                poi.HasPlayed = false;
                poi.LastTriggered = null;
                poi.LastDistance = double.MaxValue;
                System.Diagnostics.Debug.WriteLine($"[GEOFENCE] Reset POI: '{poi.Name}'");
            }
        }

        // -------------------------------------------------------------------------
        // Clustering filter
        // -------------------------------------------------------------------------

        /// <summary>
        /// Reduces POI density by removing POIs that are closer than
        /// MinimumPOISpacingMeters to a higher-priority POI already in the result set.
        /// POIs are processed in descending priority order so that higher-priority
        /// restaurants are always preserved.
        /// </summary>
        private List<POI> ApplyClusteringFilter(List<POI> pois)
        {
            // Process highest-priority POIs first
            var sorted = pois.OrderByDescending(p => p.Priority).ToList();
            var result = new List<POI>();

            foreach (var candidate in sorted)
            {
                bool tooClose = result.Any(kept =>
                    CalculateDistanceInMeters(
                        candidate.Latitude, candidate.Longitude,
                        kept.Latitude, kept.Longitude) < MinimumPOISpacingMeters);

                if (tooClose)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[CLUSTER] Filtered: '{candidate.Name}' (Priority {candidate.Priority}) — within {MinimumPOISpacingMeters}m of existing POI");
                }
                else
                {
                    result.Add(candidate);
                    System.Diagnostics.Debug.WriteLine(
                        $"[CLUSTER] Kept:     '{candidate.Name}' (Priority {candidate.Priority})");
                }
            }

            return result;
        }

        // -------------------------------------------------------------------------
        // Distance helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Haversine formula — returns accurate distance in meters between two lat/lon points.
        /// </summary>
        private static double CalculateDistanceInMeters(
            double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371.0;
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c * 1000;
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
    }
}
