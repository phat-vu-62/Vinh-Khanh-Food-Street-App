using System.Windows.Input;
using FoodStreetApp.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FoodStreetApp.ViewModels
{
    [QueryProperty(nameof(Place), "FoodPlace")]
    public class FoodDetailViewModel : INotifyPropertyChanged
    {
        private FoodPlace? _place;

        public event PropertyChangedEventHandler? PropertyChanged;

        public FoodPlace? Place
        {
            get => _place;
            set
            {
                _place = value;
                OnPropertyChanged();
            }
        }

        public ICommand ShowOnMapCommand { get; }

        public FoodDetailViewModel()
        {
            ShowOnMapCommand = new Command(async () =>
            {
                if (Place == null) return;

                string latStr = Place.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lonStr = Place.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                // Navigate to Map tab and pass coordinates
                await Shell.Current.GoToAsync($"///MapPage?lat={latStr}&lon={lonStr}");
            });
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}