using Android.App;
using Android.Content.PM;
using Android.OS;

namespace FoodStreetApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Request location permissions at startup
            RequestLocationPermissions();
        }

        private void RequestLocationPermissions()
        {
            try
            {
                // For Android 13+ (API 33+), request notification permission
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                {
                    if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Permission.Granted)
                    {
                        RequestPermissions(new[] { Android.Manifest.Permission.PostNotifications }, 100);
                    }
                }

                // Request location permissions
                var permissionsToRequest = new List<string>();

                if (CheckSelfPermission(Android.Manifest.Permission.AccessFineLocation) != Permission.Granted)
                {
                    permissionsToRequest.Add(Android.Manifest.Permission.AccessFineLocation);
                }

                if (CheckSelfPermission(Android.Manifest.Permission.AccessCoarseLocation) != Permission.Granted)
                {
                    permissionsToRequest.Add(Android.Manifest.Permission.AccessCoarseLocation);
                }

                // For Android 10+ (API 29+), request background location separately
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                {
                    if (CheckSelfPermission(Android.Manifest.Permission.AccessBackgroundLocation) != Permission.Granted)
                    {
                        // Request after foreground permissions are granted
                        permissionsToRequest.Add(Android.Manifest.Permission.AccessBackgroundLocation);
                    }
                }

                if (permissionsToRequest.Count > 0)
                {
                    RequestPermissions(permissionsToRequest.ToArray(), 101);
                    System.Diagnostics.Debug.WriteLine($">>> Requesting {permissionsToRequest.Count} permissions");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(">>> All permissions already granted");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> Error requesting permissions: {ex.Message}");
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            try
            {
                System.Diagnostics.Debug.WriteLine($">>> OnRequestPermissionsResult: requestCode={requestCode}");

                for (int i = 0; i < permissions.Length; i++)
                {
                    var permission = permissions[i];
                    var granted = grantResults[i] == Permission.Granted;
                    System.Diagnostics.Debug.WriteLine($">>> Permission {permission}: {(granted ? "GRANTED" : "DENIED")}");
                }

                // If any permission is denied, show a message
                if (grantResults.Any(r => r != Permission.Granted))
                {
                    System.Diagnostics.Debug.WriteLine(">>> ⚠️ Some permissions were denied. App may not work properly.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> Error in OnRequestPermissionsResult: {ex.Message}");
            }
        }
    }
}
