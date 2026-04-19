namespace FoodStreetApp.Services
{
    public class AuthService : IAuthService
    {
        private const string KEY_DEVICE_ID = "tracking_user_id";

        public AuthService()
        {
            // Ensure a stable device ID exists
            var id = Preferences.Get(KEY_DEVICE_ID, string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                Preferences.Set(KEY_DEVICE_ID, id);
            }
        }

        public string DeviceId => Preferences.Get(KEY_DEVICE_ID, Guid.NewGuid().ToString());
    }
}
