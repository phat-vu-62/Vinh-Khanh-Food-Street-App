using System.Net.Http.Json;
using System.Text.Json;

namespace FoodStreetApp.Services
{
    public interface ITrackingService
    {
        Task TrackEventAsync(int poiId, string action, int? durationSeconds = null, string? qrCode = null);
    }

    public class TrackingService : ITrackingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _userId;

        public TrackingService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            
            // Generate a GUID for anonymous user tracking if not already present
            _userId = Preferences.Get("tracking_user_id", string.Empty);
            if (string.IsNullOrEmpty(_userId))
            {
                _userId = Guid.NewGuid().ToString();
                Preferences.Set("tracking_user_id", _userId);
            }
        }

        public async Task TrackEventAsync(int poiId, string action, int? durationSeconds = null, string? qrCode = null)
        {
            try
            {
                var payload = new
                {
                    UserId = _userId,
                    PoiId = poiId,
                    Action = action,
                    DurationSeconds = durationSeconds,
                    QRCode = qrCode,
                    VisitedAtUtc = DateTime.UtcNow
                };

                // The CMS API endpoint for History
                var trackingUrl = "https://vinh-khanh-food-street-app.onrender.com/api/history";
                
                System.Diagnostics.Debug.WriteLine($"[TRACKING] Sending {action} for POI {poiId} (QR: {qrCode})");
                
                // PRODUCTION FIX: We MUST await to ensure data reaches the server
                var response = await _httpClient.PostAsJsonAsync(trackingUrl, payload);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[TRACKING] SUCCESS: {response.StatusCode} | Response: {responseBody}");
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[TRACKING] FAILURE: {response.StatusCode} | Error: {errorBody}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TRACKING] FATAL ERROR: {ex.Message}");
            }
        }
    }
}
