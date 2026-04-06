using FoodStreetApp.Models;

namespace FoodStreetApp.Services
{
    /// <summary>
    /// POC geofence service for Vinh Khanh street:
    ///   - ALL active POIs are checked on every GPS tick
    ///   - Active POIs are sorted nearest-first before evaluation
    ///   - Boundary-crossing detection: narration fires only when the user ENTERS the radius
    ///   - HasPlayed flag ensures each POI narrates exactly once per session
    ///   - Nearest POI inside the radius wins; priority as tiebreaker when distances are equal
    ///   - Distance-spacing guard: next narration is only allowed once the user has moved
    ///     at least <see cref="MinimumSpacingMeters"/> from the last narrated POI, preventing
    ///     rapid re-triggering while ensuring no nearby POI is skipped
    ///   - Clustering filter is used ONLY for map-display via <see cref="GetClusteredPOIs"/>
    /// </summary>
    public class GeofenceService : IGeofenceService
    {
        private List<POI> _allPois = new();
        private List<POI> _clusteredPois = new();
        private readonly INarrationService _narrationService;
        private readonly ITrackingService _trackingService;

        // --- Narration flow state ---
        private POI? _lastNarratedPoi;

        // --- Boundary-crossing state ---
        // Tracks which POI IDs the user is currently inside.
        // Populated on entry, cleared on exit, so a crossing that happens during the
        // global narration gap is still served once the gap expires.
        private readonly HashSet<int> _enteredPoiIds = new();

        // --- POC configuration constants ---

        /// <summary>Fixed trigger radius in meters — narration fires when the user enters within this distance of a POI.</summary>
        private const double TriggerRadiusMeters = 15.0;

        /// <summary>
        /// Minimum spacing between retained POIs after clustering (meters).
        /// Two POIs closer than this keep only the higher-priority one.
        /// </summary>
        private const double MinimumPOISpacingMeters = 30.0;

        /// <summary>
        /// Minimum distance in meters the user must move from the last narrated POI before
        /// another narration can fire. Prevents rapid re-triggering on dense streets while
        /// still allowing every nearby POI to be served as the user walks past.
        /// </summary>
        private const double MinimumSpacingMeters = 15.0;

        public GeofenceService(INarrationService narrationService, ITrackingService trackingService)
        {
            _narrationService = narrationService;
            _trackingService = trackingService;
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
        ///   1. Spacing guard: skip if the user has not yet moved
        ///      <see cref="MinimumSpacingMeters"/> from the last narrated POI.
        ///      This replaces a fixed time gap and prevents skipping on dense streets:
        ///      a time gap can expire while the user is still inside an adjacent POI's
        ///      radius, causing it to be evicted from <see cref="_enteredPoiIds"/> before
        ///      it ever fires.
        ///   2. Compute distances to all active POIs, sort nearest-first, then update
        ///      boundary-crossing state. A POI joins <see cref="_enteredPoiIds"/> the moment
        ///      the user steps inside its radius and is removed when they leave.
        ///   3. Candidates = entered POIs where <see cref="POI.HasPlayed"/> is false.
        ///   4. Nearest candidate wins; priority as tiebreaker.
        ///   5. Set HasPlayed = true and fire narration.
        /// </summary>
        public POI? CheckGeofences(Location currentLocation)
        {
            // --- 1. Distance-spacing guard ---
            // Block new narration until the user has walked MinimumSpacingMeters from the
            // last narrated POI. Unlike a time gate, this tracks physical movement so no
            // POI is skipped because the user walks through it during a timer window.
            if (_lastNarratedPoi != null)
            {
                var distanceFromLast = CalculateDistanceInMeters(
                    currentLocation.Latitude, currentLocation.Longitude,
                    _lastNarratedPoi.Latitude, _lastNarratedPoi.Longitude);

                if (distanceFromLast < MinimumSpacingMeters)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[GEOFENCE] Spacing: {distanceFromLast:F0}m from '{_lastNarratedPoi.Name}' (need {MinimumSpacingMeters:F0}m to unlock next)");
                    return null;
                }
            }

            // --- 2. Distance computation, nearest-first sort, boundary-crossing update ---
            // Distances are computed once and reused for both sorting and candidate building.
            var activePois = _allPois
                .Where(p => p.IsActive)
                .Select(p => (
                    poi: p,
                    distance: CalculateDistanceInMeters(
                        currentLocation.Latitude, currentLocation.Longitude,
                        p.Latitude, p.Longitude)))
                .OrderBy(x => x.distance)
                .ToList();

            var candidates = new List<(POI poi, double distance)>();

            System.Diagnostics.Debug.WriteLine($"[GEOFENCE] Checking {activePois.Count} POIs (trigger radius {TriggerRadiusMeters:F0}m):");
            foreach (var (poi, currentDistance) in activePois)
            {
                var previousDistance = poi.LastDistance;
                poi.LastDistance = currentDistance;

                var isNowInside = currentDistance <= TriggerRadiusMeters;
                var wasOutside  = previousDistance  > TriggerRadiusMeters;

                // Maintain entered-set: add on inward crossing, remove when outside
                if (isNowInside && wasOutside)
                {
                    _enteredPoiIds.Add(poi.Id);
                    _ = _trackingService.TrackEventAsync(poi.Id, "Route_Entry");
                }
                else if (!isNowInside)
                    _enteredPoiIds.Remove(poi.Id);

                var isEntered = _enteredPoiIds.Contains(poi.Id);

                var tag = isEntered && !poi.HasPlayed ? "✅ IN " : isEntered ? "🔇 PLY" : "📏 OUT";
                System.Diagnostics.Debug.WriteLine(
                    $"  [{tag}] '{poi.Name}' {currentDistance:F0}m | P{poi.Priority}");

                if (isEntered && !poi.HasPlayed)
                    candidates.Add((poi, currentDistance));
            }

            // No POIs entered or all already played
            if (candidates.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[GEOFENCE] No candidates in range");
                return null;
            }

            // --- 3. Nearest POI wins; priority as tiebreaker ---
            var best = candidates
                .OrderBy(c => c.distance)
                .ThenByDescending(c => c.poi.Priority)
                .First();

            // --- 4. Trigger ---
            System.Diagnostics.Debug.WriteLine(
                $"[TRIGGER] ▶ '{best.poi.Name}' at {best.distance:F0}m (Priority: {best.poi.Priority})");

            best.poi.HasPlayed = true;
            best.poi.LastTriggered = DateTime.UtcNow;
            _lastNarratedPoi = best.poi;

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
            _enteredPoiIds.Clear();
            _lastNarratedPoi = null;
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
                _enteredPoiIds.Remove(poiId);
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
