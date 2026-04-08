using System.Net.Http.Json;

namespace FoodStreetApp.Services
{
    public interface ITrackingService
    {
        Task TrackEventAsync(int poiId, string action, int? durationSeconds = null);
    }

    public class TrackingService : ITrackingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _userId;

        public TrackingService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            
            // Generate a GUID for anonymous user tracking if not already present
            _userId = Preferences.Get("tracking_user_id", string.Empty);
            if (string.IsNullOrEmpty(_userId))
            {
                _userId = Guid.NewGuid().ToString();
                Preferences.Set("tracking_user_id", _userId);
            }
        }

        public async Task TrackEventAsync(int poiId, string action, int? durationSeconds = null)
        {
            try
            {
                var payload = new
                {
                    UserId = _userId,
                    PoiId = poiId,
                    Action = action,
                    DurationSeconds = durationSeconds,
                    VisitedAtUtc = DateTime.UtcNow
                };

                // The CMS API endpoint for UsageHistory
                var trackingUrl = "https://vinh-khanh-food-street-app.onrender.com/api/history";
                
                System.Diagnostics.Debug.WriteLine($"[TRACKING] Sending {action} for POI {poiId} (Duration: {durationSeconds}s)");
                
                // Fire and forget, don't await to avoid blocking narration or geofence
                _ = _httpClient.PostAsJsonAsync(trackingUrl, payload).ContinueWith(t => 
                {
                    if (t.IsFaulted)
                        System.Diagnostics.Debug.WriteLine($"[TRACKING] Failed: {t.Exception?.InnerException?.Message}");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TRACKING] Error queuing event: {ex.Message}");
            }
        }
    }
}
