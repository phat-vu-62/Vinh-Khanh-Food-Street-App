using Microsoft.Extensions.DependencyInjection;

namespace FoodStreetApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        private IDispatcherTimer? _heartbeatTimer;

        protected override void OnStart()
        {
            base.OnStart();

            try
            {
                StartHeartbeatTimer();
                // Send immediate ping so user shows Online right away
                SendImmediatePing();

                var bgEnabled = Preferences.Get("background_tracking", false);
                if (bgEnabled)
                {
#if ANDROID
                    try
                    {
                        var intent = new Android.Content.Intent(Platform.AppContext, typeof(FoodStreetApp.Platforms.Android.Services.LocationBackgroundService));
                        Platform.AppContext.StartForegroundService(intent);
                        System.Diagnostics.Debug.WriteLine("[APP] Background service started from OnStart");
                    }
                    catch (Exception bgEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[APP] Failed to start BG service: {bgEx.Message}");
                    }
#endif
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APP] OnStart error: {ex.Message}");
            }
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            var bgTrackingEnabled = Preferences.Get("background_tracking", false);
            if (!bgTrackingEnabled)
            {
                // No background tracking — stop heartbeat, user will show offline
                _heartbeatTimer?.Stop();
                System.Diagnostics.Debug.WriteLine("[APP] Heartbeat stopped (OnSleep, no background tracking)");
            }
            else
            {
                // Background tracking ON — keep UI heartbeat running as backup
                // MAUI dispatcher may still fire in background on some devices
                // Background service also sends pings independently
                System.Diagnostics.Debug.WriteLine("[APP] Heartbeat continues (OnSleep, background tracking ON)");
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            // Restart heartbeat when app comes back to foreground
            _heartbeatTimer?.Start();
            // Send immediate ping so Online shows instantly
            SendImmediatePing();
            System.Diagnostics.Debug.WriteLine("[APP] Heartbeat resumed (OnResume)");
        }

        private void SendImmediatePing()
        {
            Task.Run(async () =>
            {
                try
                {
                    var tracker = GetTrackingService();
                    if (tracker != null)
                    {
                        await tracker.TrackEventAsync(0, "app_ping");
                        System.Diagnostics.Debug.WriteLine("[APP] Immediate ping sent");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[APP] Immediate ping skipped — tracker is null");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[APP] Immediate ping error: {ex.Message}");
                }
            });
        }

        private void StartHeartbeatTimer()
        {
            try
            {
                if (Application.Current?.Dispatcher == null) return;
                
                _heartbeatTimer = Application.Current.Dispatcher.CreateTimer();
                _heartbeatTimer.Interval = TimeSpan.FromSeconds(2);
                _heartbeatTimer.Tick += async (s, e) =>
                {
                    try
                    {
                        var tracker = GetTrackingService();
                        if (tracker != null)
                        {
                            await tracker.TrackEventAsync(0, "app_ping");
                        }
                    }
                    catch { /* Services chưa sẵn sàng */ }
                };
                _heartbeatTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APP] Heartbeat timer error: {ex.Message}");
            }
        }

        /// <summary>
        /// Reliable way to get ITrackingService — tries multiple resolution paths
        /// </summary>
        private Services.ITrackingService? GetTrackingService()
        {
            // Try 1: IPlatformApplication (most reliable, works early in lifecycle)
            try
            {
                var svc = IPlatformApplication.Current?.Services?.GetService<Services.ITrackingService>();
                if (svc != null) return svc;
            }
            catch { }

            // Try 2: Handler.MauiContext (works after window is created)
            try
            {
                var svc = Handler?.MauiContext?.Services.GetService<Services.ITrackingService>();
                if (svc != null) return svc;
            }
            catch { }

            return null;
        }


        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        protected override async void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            if (uri.Scheme == "foodstreet" && uri.Host == "poi")
            {
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var idStr = query.Get("id");
                var play = query.Get("play") == "true";
                var skipGps = query.Get("skipGps") == "true";

                if (int.TryParse(idStr, out var id))
                {
                    var repo = Handler?.MauiContext?.Services.GetService<Data.IPOIRepository>();
                    if (repo != null)
                    {
                        var poi = await repo.GetPOIByIdAsync(id);
                        if (poi != null)
                        {
                            var place = new Models.FoodPlace
                            {
                                Name = poi.Name,
                                Description = poi.Description ?? "",
                                Latitude = poi.Latitude,
                                Longitude = poi.Longitude,
                                ImageUrl = poi.ImageUrl,
                                PoiData = poi
                            };
                            
                            // Navigation will now trigger QR tracking via QueryProperty in FoodDetailViewModel


                            // Navigate to detail page with autoplay parameters
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                var encodedQr = Uri.EscapeDataString(uri.ToString());
                                await Shell.Current.GoToAsync($"{nameof(Views.FoodDetailPage)}?AutoPlay={play}&SkipGps={skipGps}&QRCode={encodedQr}", 
                                    new Dictionary<string, object> { { "FoodPlace", place } });
                            });


                        }
                    }
                }
            }
        }
    }
}