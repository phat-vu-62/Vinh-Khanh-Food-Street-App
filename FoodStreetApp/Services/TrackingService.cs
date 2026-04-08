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
            // Configure HttpClient with SSL bypass for Android/iOS if needed (Render/Cert issues)
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

            _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            
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
                // BACKEND SOURCE OF TRUTH: We no longer send VisitedAtUtc from the client
                var payload = new
                {
                    UserId = _userId,
                    PoiId = poiId,
                    Action = action,
                    DurationSeconds = durationSeconds,
                    QRCode = qrCode
                };

                // UPDATED PRODUCTION URL: Ensuring we hit the correct endpoint on the CMS Host
                var trackingUrl = "https://vinh-khanh-food-street-app.onrender.com/api/UsageHistory";
                
                // Detailed debug logging as requested
                System.Diagnostics.Debug.WriteLine($"[DEBUG-TRACK] SENDING: {action} (POI:{poiId}, QR:{qrCode ?? "None"})");
                
                var response = await _httpClient.PostAsJsonAsync(trackingUrl, payload);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[DEBUG-TRACK] SUCCESS: {response.StatusCode}");
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[DEBUG-TRACK] FAILURE: {response.StatusCode} | Error: {errorBody}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG-TRACK] FATAL ERROR: {ex.Message}");
            }
        }
    }
}
