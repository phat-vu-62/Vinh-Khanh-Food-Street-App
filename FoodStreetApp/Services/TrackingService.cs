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
                
                Console.WriteLine($"[TRACKING] Sending {action} for POI {poiId} (QR: {qrCode})");
                
                // PRODUCTION FIX: No more fire-and-forget. We must await to ensure data is sent.
                var response = await _httpClient.PostAsJsonAsync(trackingUrl, payload);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[TRACKING] SUCCESS: {response.StatusCode} | Full Response: {responseBody}");
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[TRACKING] FAILURE: {response.StatusCode} | Error: {errorBody}");
                    
                    // Specific log for QR scanned to alert user
                    if (action == "qr_scanned")
                    {
                        System.Diagnostics.Debug.WriteLine("[TRACKING] CRITICAL: QR scan was NOT recorded by server.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TRACKING] FATAL ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TRACKING] Exception Details: {ex}");
            }
        }
    }
}
