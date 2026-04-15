using System.Net.Http.Json;
using System.Text.Json;

namespace FoodStreetApp.Services
{
    public class AuthService : IAuthService
    {
        private const string KEY_USER_ID = "logged_in_user_id";
        private const string KEY_USERNAME = "logged_in_username";
        private const string KEY_FULLNAME = "logged_in_fullname";

        private static readonly string BaseUrl = "https://vinh-khanh-food-street-app.onrender.com";
        private readonly HttpClient _httpClient;

        public AuthService()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(Preferences.Get(KEY_USER_ID, string.Empty));
        public string? CurrentUserId => Preferences.Get(KEY_USER_ID, null);
        public string? CurrentUsername => Preferences.Get(KEY_USERNAME, null);
        public string? CurrentFullName => Preferences.Get(KEY_FULLNAME, null);

        public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
        {
            try
            {
                var payload = new { Username = username, Password = password };
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/auth/api-login", payload);
                var json = await response.Content.ReadAsStringAsync();
                
                string errorMsg = "Đăng nhập thất bại.";
                try
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(json);
                    if (response.IsSuccessStatusCode && result.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        var userId = result.GetProperty("userId").GetString()!;
                        var user = result.GetProperty("username").GetString()!;
                        var fullName = result.TryGetProperty("fullName", out var fn) ? fn.GetString() : user;

                    Preferences.Set(KEY_USER_ID, userId);
                    Preferences.Set(KEY_USERNAME, user);
                    Preferences.Set(KEY_FULLNAME, fullName ?? user);

                    // Also update tracking_user_id so TrackingService picks up the real ID
                    Preferences.Set("tracking_user_id", userId);

                    System.Diagnostics.Debug.WriteLine($"[AUTH] Login OK: {user} ({userId})");
                    return (true, "Đăng nhập thành công!");
                }

                    errorMsg = result.TryGetProperty("message", out var m) ? m.GetString() ?? errorMsg : errorMsg;
                }
                catch
                {
                    errorMsg = string.IsNullOrWhiteSpace(json) ? errorMsg : json;
                }

                return (false, errorMsg);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] Login error: {ex.Message}");
                return (false, $"Lỗi kết nối: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string username, string password, string? email, string? fullName)
        {
            try
            {
                var payload = new { Username = username, Password = password, Email = email, FullName = fullName };
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/auth/api-register", payload);
                var json = await response.Content.ReadAsStringAsync();
                
                string errorMsg = "Đăng ký thất bại.";
                try
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(json);

                    if (response.IsSuccessStatusCode && result.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                    var msg = result.TryGetProperty("message", out var m) ? m.GetString() : "Đăng ký thành công!";
                    System.Diagnostics.Debug.WriteLine($"[AUTH] Register OK: {username}");
                    return (true, msg ?? "Đăng ký thành công!");
                }

                    errorMsg = result.TryGetProperty("message", out var em) ? em.GetString() ?? errorMsg : errorMsg;
                }
                catch
                {
                    errorMsg = string.IsNullOrWhiteSpace(json) ? errorMsg : json;
                }

                return (false, errorMsg);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTH] Register error: {ex.Message}");
                return (false, $"Lỗi kết nối: {ex.Message}");
            }
        }

        public void Logout()
        {
            // Restore anonymous device tracking
            var newDeviceId = Guid.NewGuid().ToString();
            Preferences.Remove(KEY_USER_ID);
            Preferences.Remove(KEY_USERNAME);
            Preferences.Remove(KEY_FULLNAME);
            Preferences.Set("tracking_user_id", newDeviceId);

            System.Diagnostics.Debug.WriteLine("[AUTH] Logged out, reverted to anonymous tracking.");
        }
    }
}
