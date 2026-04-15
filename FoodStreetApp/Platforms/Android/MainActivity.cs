using Android.App;
using Android.Content.PM;
using Android.OS;

namespace FoodStreetApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { Android.Content.Intent.ActionView },
                  Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
                  DataScheme = "foodstreet",
                  DataHost = "poi")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Ngăn crash trên Android khi mở lại app
            Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[ANDROID-CRASH] {e.Exception.Message}");
                e.Handled = true; // Ngăn Force Close
            };
        }

        // MAUI's MauiAppCompatActivity base class hooks into OnRequestPermissionsResult
        // to complete Permissions.RequestAsync calls. Forwarding to base is all that is needed.
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
