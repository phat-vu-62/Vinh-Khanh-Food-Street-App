namespace FoodStreetApp.Services
{
    public class LocationService : ILocationService
    {
        private CancellationTokenSource? _cancelTokenSource;
        private bool _isTracking = false;

        public event EventHandler<Location>? LocationChanged;

        public async Task<Location?> GetCurrentLocationAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(">>> Requesting GPS location...");

                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(1));

                // This will use mock location if enabled in device settings
                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    var ageSeconds = (DateTimeOffset.UtcNow - location.Timestamp).TotalSeconds;
                    System.Diagnostics.Debug.WriteLine($">>> GPS: {location.Latitude:F6}, {location.Longitude:F6} | Accuracy: {location.Accuracy:F1}m | Age: {ageSeconds:F0}s");
                    if (ageSeconds > 5)
                        System.Diagnostics.Debug.WriteLine($">>> ⚠️ STALE LOCATION ({ageSeconds:F0}s old) — verify emulator mock location is active");
#if ANDROID
                    if (location.IsFromMockProvider)
                        System.Diagnostics.Debug.WriteLine(">>> ⚠️ MOCK location (emulator Extended Controls)");
                    else
                        System.Diagnostics.Debug.WriteLine(">>> 📍 Real GPS location");
#endif
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(">>> ⚠️ GPS location is null — check permissions and mock location settings");
                }

                return location;
            }
            catch (FeatureNotSupportedException ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> ❌ GPS not supported: {ex.Message}");
                return null;
            }
            catch (PermissionException ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> ❌ GPS permission denied: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> ❌ Unable to get location: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($">>> Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<bool> StartTrackingAsync()
        {
            if (_isTracking)
            {
                System.Diagnostics.Debug.WriteLine(">>> Location tracking already running");
                return true;
            }

            try
            {
                _cancelTokenSource = new CancellationTokenSource();
                _isTracking = true;

                System.Diagnostics.Debug.WriteLine(">>> Starting location tracking...");

                // Fire-and-forget tracking loop (don't await)
                _ = Task.Run(async () =>
                {
                    System.Diagnostics.Debug.WriteLine(">>> Location tracking loop started");

                    while (!_cancelTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var location = await GetCurrentLocationAsync();
                            if (location != null)
                            {
                                System.Diagnostics.Debug.WriteLine($">>> Location tracked: {location.Latitude:F6}, {location.Longitude:F6} (Accuracy: {location.Accuracy}m)");
                                LocationChanged?.Invoke(this, location);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine(">>> Location is null, retrying...");
                            }

                            var intervalMs = Preferences.Get("update_frequency", 5) * 1000;
                            await Task.Delay(intervalMs, _cancelTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            System.Diagnostics.Debug.WriteLine(">>> Location tracking cancelled");
                            break;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($">>> Location tracking error: {ex.Message}");
                            await Task.Delay(1000); // Wait before retry
                        }
                    }

                    System.Diagnostics.Debug.WriteLine(">>> Location tracking loop ended");
                }, _cancelTokenSource.Token);

                // Give it a moment to start
                await Task.Delay(100);

                System.Diagnostics.Debug.WriteLine(">>> Location tracking started successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> Error starting location tracking: {ex.Message}");
                _isTracking = false;
                return false;
            }
        }

        public Task StopTrackingAsync()
        {
            if (_cancelTokenSource != null && !_cancelTokenSource.IsCancellationRequested)
            {
                _cancelTokenSource.Cancel();
            }
            _isTracking = false;
            return Task.CompletedTask;
        }
    }
}
