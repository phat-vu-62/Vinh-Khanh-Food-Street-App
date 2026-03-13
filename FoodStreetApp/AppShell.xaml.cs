using FoodStreetApp.Views;

namespace FoodStreetApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(FoodDetailPage), typeof(FoodDetailPage));
        }
    }
}
