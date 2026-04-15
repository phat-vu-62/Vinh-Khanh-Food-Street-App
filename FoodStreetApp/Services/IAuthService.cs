namespace FoodStreetApp.Services
{
    public interface IAuthService
    {
        bool IsLoggedIn { get; }
        string? CurrentUserId { get; }
        string? CurrentUsername { get; }
        string? CurrentFullName { get; }

        Task<(bool Success, string Message)> LoginAsync(string username, string password);
        Task<(bool Success, string Message)> RegisterAsync(string username, string password, string? email, string? fullName, string? phoneNumber = null);
        void Logout();
    }
}
