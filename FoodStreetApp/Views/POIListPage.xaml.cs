using FoodStreetApp.ViewModels;
using FoodStreetApp.Models;

namespace FoodStreetApp.Views
{
    public partial class POIListPage : ContentPage
    {
        private readonly POIListViewModel _viewModel;

        public POIListPage(POIListViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadPOIsAsync();
        }
    }
}
