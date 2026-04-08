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
            // PRO-GRADE DEBUG: Configure HttpClient with SSL bypass for Android/iOS if needed
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
            var payload = new
            {
                UserId = _userId,
                PoiId = poiId,
                Action = action,
                DurationSeconds = durationSeconds,
                QRCode = qrCode,
                VisitedAtUtc = DateTime.UtcNow
            };

            string json = JsonSerializer.Serialize(payload);
            string url = "https://vinh-khanh-food-street-app.onrender.com/api/UsageHistory";


            System.Diagnostics.Debug.WriteLine("====================================");
            System.Diagnostics.Debug.WriteLine("[DEBUG-TRACK] ATTEMPTING SEND");
            System.Diagnostics.Debug.WriteLine($"[DEBUG-TRACK] URL: {url}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG-TRACK] PAYLOAD: {json}");
            System.Diagnostics.Debug.WriteLine("====================================");

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, payload);

                System.Diagnostics.Debug.WriteLine($"[DEBUG-TRACK] RESPONSE RECEIVED: {response.StatusCode}");
                
                var responseBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG-TRACK] RESPONSE BODY: {responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG-TRACK] ⚠️ SERVER REFUSED: {response.ReasonPhrase}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG-TRACK] ✅ SUCCESS: Record should be in DB.");
                }
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG-TRACK] ❌ NETWORK ERROR: {httpEx.Message}");
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG-TRACK] ❌ TIMEOUT: Server took > 30s");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG-TRACK] ❌ UNEXPECTED ERROR: {ex.GetType().Name} - {ex.Message}");
            }
            System.Diagnostics.Debug.WriteLine("====================================");
        }
    }
}
