using FoodStreetApp.ViewModels;
using FoodStreetApp.Services;

namespace FoodStreetApp.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Refresh account state whenever user comes back from Login/Register
            _viewModel.RefreshAccountState();
        }

        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(LoginPage));
        }

        private void OnLogoutClicked(object? sender, EventArgs e)
        {
            var authService = Handler?.MauiContext?.Services.GetService<IAuthService>();
            authService?.Logout();
            _viewModel.RefreshAccountState();
        }
    }
}
