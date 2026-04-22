namespace FoodStreetApp.Services
{
    public interface ILocationService
    {
        Task<Location?> GetCurrentLocationAsync();  
        Task<bool> StartTrackingAsync();
        Task StopTrackingAsync();
        event EventHandler<Location>? LocationChanged;
    }
}
