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
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                var location = await Geolocation.Default.GetLocationAsync(request);
                return location;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unable to get location: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> StartTrackingAsync()
        {
            if (_isTracking)
                return true;

            try
            {
                _cancelTokenSource = new CancellationTokenSource();
                _isTracking = true;

                await Task.Run(async () =>
                {
                    while (!_cancelTokenSource.Token.IsCancellationRequested)
                    {
                        var location = await GetCurrentLocationAsync();
                        if (location != null)
                        {
                            LocationChanged?.Invoke(this, location);
                        }

                        await Task.Delay(5000, _cancelTokenSource.Token);
                    }
                }, _cancelTokenSource.Token);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting location tracking: {ex.Message}");
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
