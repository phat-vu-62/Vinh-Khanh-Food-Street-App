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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APP] OnStart error: {ex.Message}");
            }
        }

        private void StartHeartbeatTimer()
        {
            try
            {
                if (Application.Current?.Dispatcher == null) return;
                
                _heartbeatTimer = Application.Current.Dispatcher.CreateTimer();
                _heartbeatTimer.Interval = TimeSpan.FromSeconds(2);
                _heartbeatTimer.Tick += (s, e) =>
                {
                    try
                    {
                        // Always send heartbeat — app is anonymous, no login check needed
                        var tracker = Handler?.MauiContext?.Services.GetService<Services.ITrackingService>();
                        tracker?.TrackEventAsync(0, "app_ping");
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