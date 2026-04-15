using FoodStreetApp.ViewModels;

namespace FoodStreetApp.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        private void OnUsernameCompleted(object? sender, EventArgs e)
        {
            PasswordEntry.Focus();
        }

        private async void OnBackTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
