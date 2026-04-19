namespace FoodStreetApp.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Anonymous device ID used for tracking. Auto-generated on first use.
        /// </summary>
        string DeviceId { get; }
    }
}
