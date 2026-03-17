using Microsoft.Maui.Controls;
using FoodStreetApp.ViewModels;

namespace FoodStreetApp.Views
{
    public partial class HomePage : ContentPage
    {
        private HomeViewModel _viewModel;

        public HomePage(HomeViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel.AllRestaurants.Count == 0)
            {
                _viewModel.LoadDataCommand.Execute(null);
            }
        }
    }
}