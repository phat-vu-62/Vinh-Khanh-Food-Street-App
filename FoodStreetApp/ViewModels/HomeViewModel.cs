using System.Collections.ObjectModel;
using System.Windows.Input;
using FoodStreetApp.Models;
using FoodStreetApp.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FoodStreetApp.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private readonly IPOIService _poiService;
        private string _searchText = string.Empty;

        public ObservableCollection<FoodPlace> FeaturedStalls { get; }
        public ObservableCollection<FoodPlace> PopularSeafoodStalls { get; }

        public ObservableCollection<FoodPlace> AllRestaurants { get; } = new();
        public ObservableCollection<FoodPlace> SearchResults { get; } = new();
        private List<FoodPlace> _allRestaurantsFullList = new();

        private bool _isSearching;
        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                if (_isSearching != value)
                {
                    _isSearching = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand GoToDetailCommand { get; }
        public ICommand LoadDataCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    FilterRestaurants();
                }
            }
        }

        public HomeViewModel(IPOIService poiService)
        {
            _poiService = poiService;
            FeaturedStalls = new ObservableCollection<FoodPlace>
            {
                new FoodPlace
                {
                    Name = "Ốc Phát",
                    Image = "ocphat.jpg",
                    Rating = 4.8,
                    Description = "Quán ốc bình dân, đa dạng các loại ốc tươi ngon, nêm nếm đậm đà.",
                    Latitude = 10.7621,
                    Longitude = 106.7032
                },
                new FoodPlace
                {
                    Name = "Ốc Hồng Nhung",
                    Image = "ochongnhung.jpg",
                    Rating = 4.5,
                    Description = "Nổi tiếng với các món ốc xào me, nướng mỡ hành thơm lừng.",
                    Latitude = 10.7615,
                    Longitude = 106.7038
                },
                new FoodPlace
                {
                    Name = "BONA Food and Beer",
                    Image = "bona.jpg",
                    Rating = 4.9,
                    Description = "Kết hợp giữa đồ ăn ngon và bia tươi, cực kỳ sôi động về đêm.",
                    Latitude = 10.7598,
                    Longitude = 106.7055
                }
            };

            PopularSeafoodStalls = new ObservableCollection<FoodPlace>
            {
                new FoodPlace
                {
                    Name = "Ốc Nhi 20k",
                    Image = "ocnhi20k.jpg",
                    Rating = 4.6,
                    Description = "Đồng giá 20k, phù hợp học sinh sinh viên, ngon và rẻ.",
                    Latitude = 10.7610,
                    Longitude = 106.7042
                },
                new FoodPlace
                {
                    Name = "Ốc Ty",
                    Image = "octy.jpg",
                    Rating = 4.7,
                    Description = "Ốc tươi sống, phục vụ nhanh, không gian thoáng mát.",
                    Latitude = 10.7605,
                    Longitude = 106.7050
                },
                new FoodPlace
                {
                    Name = "Lãng Quán",
                    Image = "langquan.jpg",
                    Rating = 4.4,
                    Description = "Hải sản tươi sống, không gian gia đình ấm cúng.",
                    Latitude = 10.7592,
                    Longitude = 106.7061
                }
            };

            GoToDetailCommand = new Command<FoodPlace>(async (place) =>
            {
                if (place == null) return;

                var navigationParameter = new Dictionary<string, object>
                {
                    { "FoodPlace", place }
                };

                await Shell.Current.GoToAsync("FoodDetailPage", navigationParameter);
            });

            LoadDataCommand = new Command(async () => await LoadAllRestaurantsAsync());
        }

        private async Task LoadAllRestaurantsAsync()
        {
            try
            {
                var pois = await _poiService.GetAllPOIsAsync();
                _allRestaurantsFullList.Clear();

                foreach (var poi in pois)
                {
                    // Basic image mapping based on existing assets if possible or generic
                    string imageName = "placeholder_food.png";
                    if (poi.Name.Contains("Phát")) imageName = "ocphat.jpg";
                    else if (poi.Name.Contains("Hồng Nhung")) imageName = "ochongnhung.jpg";
                    else if (poi.Name.Contains("BONA")) imageName = "bona.jpg";
                    else if (poi.Name.Contains("Nhi 20k")) imageName = "ocnhi20k.jpg";
                    else if (poi.Name.Contains("Ty")) imageName = "octy.jpg";
                    else if (poi.Name.Contains("Lãng Quán")) imageName = "langquan.jpg";
                    else if (poi.Name.Contains("Win")) imageName = "wincoffee.jpg";
                    else if (poi.Name.Contains("Thảo")) imageName = "octhao.jpg";
                    else if (poi.Name.Contains("Vũ")) imageName = "ocvu.jpg";
                    else if (poi.Name.Contains("Oanh")) imageName = "ocoanh.jpg";
                    else if (poi.Name.Contains("A FAT")) imageName = "afathotpot.jpg";
                    else if (poi.Name.Contains("Bụi")) imageName = "ocbui.jpg";
                    else if (poi.Name.Contains("Đào 2")) imageName = "ocdao2.jpg";
                    else if (poi.Name.Contains("Diễm")) imageName = "ocdiem.jpg";
                    else if (poi.Name.Contains("Lẩu gà lá é")) imageName = "laugalae.jpg";

                    var place = new FoodPlace
                    {
                        Name = poi.Name,
                        Image = imageName,
                        Rating = poi.Rating, // DB Rating
                        Description = poi.Description,
                        Latitude = poi.Latitude,
                        Longitude = poi.Longitude,
                        PoiData = poi
                    };

                    _allRestaurantsFullList.Add(place);

                    // Also try to link this real POI back to the Featured/Popular/Drinks lists so audio is accurate
                    var featured = FeaturedStalls.FirstOrDefault(f => f.Name == poi.Name);
                    if (featured != null)
                    {
                        featured.PoiData = poi;
                        featured.Latitude = poi.Latitude;
                        featured.Longitude = poi.Longitude;
                        featured.Rating = poi.Rating;
                    }

                    var popular = PopularSeafoodStalls.FirstOrDefault(p => p.Name == poi.Name);
                    if (popular != null)
                    {
                        popular.PoiData = poi;
                        popular.Latitude = poi.Latitude;
                        popular.Longitude = poi.Longitude;
                        popular.Rating = poi.Rating;
                    }
                }

                // Initial populate
                AllRestaurants.Clear();
                foreach (var item in _allRestaurantsFullList)
                {
                    AllRestaurants.Add(item);
                }
                FilterRestaurants();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading ALL POIs for Home: {ex.Message}");
            }
        }

        private void FilterRestaurants()
        {
            SearchResults.Clear();
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                IsSearching = false;
                return;
            }

            IsSearching = true;
            var filtered = _allRestaurantsFullList
                .Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var item in filtered)
            {
                SearchResults.Add(item);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}