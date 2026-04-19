using Android.App;
using Android.Content;
using Android.OS;
using Android.Content.PM;
using AndroidX.Core.App;
using FoodStreetApp.Services;

namespace FoodStreetApp.Platforms.Android.Services
{
    [Service(ForegroundServiceType = ForegroundService.TypeLocation)]
    public class LocationBackgroundService : Service
    {
        private const int NotificationId = 1000;
        private const string ChannelId = "location_service_channel";
        private ILocationService? _locationService;
        private CancellationTokenSource? _pingCts;

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(">>> LocationBackgroundService: OnStartCommand");

                CreateNotificationChannel();

                // Try to get app icon, fallback to default if not available
                int iconResId;
                try
                {
                    iconResId = Resource.Drawable.notification_icon_background;
                }
                catch
                {
                    // Fallback to app icon if notification_icon_background doesn't exist
                    try
                    {
                        iconResId = ApplicationInfo.Icon;
                    }
                    catch
                    {
                        // Ultimate fallback - use a system icon
                        iconResId = global::Android.Resource.Drawable.IcMenuMyLocation;
                    }
                }

                var notification = new NotificationCompat.Builder(this, ChannelId)
                    .SetContentTitle("Food Street Guide")
                    .SetContentText("Tracking your location for nearby POIs")
                    .SetSmallIcon(iconResId)
                    .SetOngoing(true)
                    .SetPriority(NotificationCompat.PriorityLow)
                    .Build();

                StartForeground(NotificationId, notification);
                System.Diagnostics.Debug.WriteLine(">>> LocationBackgroundService: Foreground service started");

                // Start location tracking
                Task.Run(async () =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine(">>> LocationBackgroundService: Getting service provider...");
                        var serviceProvider = IPlatformApplication.Current?.Services;
                        if (serviceProvider != null)
                        {
                            _locationService = serviceProvider.GetService<ILocationService>();
                            if (_locationService != null)
                            {
                                System.Diagnostics.Debug.WriteLine(">>> LocationBackgroundService: Starting location tracking...");
                                await _locationService.StartTrackingAsync();
                                System.Diagnostics.Debug.WriteLine(">>> LocationBackgroundService: Location tracking started");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine(">>> LocationBackgroundService: ⚠️ LocationService is null");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine(">>> LocationBackgroundService: ⚠️ Service provider is null");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($">>> LocationBackgroundService: ❌ Error starting tracking: {ex.Message}");
                    }
                });

                // Start background heartbeat ping (runs on background thread, not UI dispatcher)
                StartBackgroundHeartbeat();

                return StartCommandResult.Sticky;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> LocationBackgroundService: ❌ OnStartCommand error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($">>> Stack trace: {ex.StackTrace}");
                return StartCommandResult.NotSticky;
            }
        }

        private void StartBackgroundHeartbeat()
        {
            _pingCts?.Cancel();
            _pingCts = new CancellationTokenSource();
            var token = _pingCts.Token;

            Task.Run(async () =>
            {
                System.Diagnostics.Debug.WriteLine("[BG-HEARTBEAT] Starting background ping loop");
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (msg, cert, chain, err) => true
                };
                using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
                var userId = Preferences.Get("tracking_user_id", string.Empty);
                var trackingUrl = "https://vinh-khanh-food-street-app.onrender.com/api/history";

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var payload = new { UserId = userId, PoiId = 0, Action = "app_ping" };
                        await http.PostAsJsonAsync(trackingUrl, payload);
                        System.Diagnostics.Debug.WriteLine("[BG-HEARTBEAT] Ping sent");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BG-HEARTBEAT] Ping failed: {ex.Message}");
                    }

                    try { await Task.Delay(2000, token); }
                    catch (TaskCanceledException) { break; }
                }
                System.Diagnostics.Debug.WriteLine("[BG-HEARTBEAT] Ping loop stopped");
            }, token);
        }

        public override void OnDestroy()
        {
            // Stop heartbeat pings
            _pingCts?.Cancel();
            _pingCts?.Dispose();
            _pingCts = null;

            _locationService?.StopTrackingAsync();
            base.OnDestroy();
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(ChannelId, "Location Service", NotificationImportance.Low)
                {
                    Description = "Tracks location for Food Street POIs"
                };

                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
            }
        }
    }
}
