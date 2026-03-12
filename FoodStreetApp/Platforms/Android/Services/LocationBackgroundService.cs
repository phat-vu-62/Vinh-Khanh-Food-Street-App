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

                return StartCommandResult.Sticky;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> LocationBackgroundService: ❌ OnStartCommand error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($">>> Stack trace: {ex.StackTrace}");
                return StartCommandResult.NotSticky;
            }
        }

        public override void OnDestroy()
        {
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
