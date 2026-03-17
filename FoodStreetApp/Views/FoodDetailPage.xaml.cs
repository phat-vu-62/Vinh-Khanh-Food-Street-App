using Microsoft.Maui.Controls;
using FoodStreetApp.ViewModels;

namespace FoodStreetApp.Views
{
    public partial class FoodDetailPage : ContentPage
    {
        public FoodDetailPage(FoodDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}