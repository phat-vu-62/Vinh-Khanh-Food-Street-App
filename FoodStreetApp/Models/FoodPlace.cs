using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FoodStreetApp.Models
{
    public class FoodPlace : INotifyPropertyChanged
    {
        private string _description = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public double Rating { get; set; }

        public string Description 
        { 
            get => _description; 
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OriginalDescription { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public POI? PoiData { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}