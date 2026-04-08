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
        private const string RenderSyncUrl = "https://vinh-khanh-food-street-app.onrender.com/api/sync/pois";
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        public ObservableCollection<FoodPlace> FeaturedStalls { get; }
        public ObservableCollection<FoodPlace> PopularSeafoodStalls { get; }

        private ObservableCollection<FoodPlace> _allRestaurants = new();
        public ObservableCollection<FoodPlace> AllRestaurants
        {
            get => _allRestaurants;
            set
            {
                if (_allRestaurants != value)
                {
                    _allRestaurants = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<FoodPlace> _searchResults = new();
        public ObservableCollection<FoodPlace> SearchResults
        {
            get => _searchResults;
            set
            {
                if (_searchResults != value)
                {
                    _searchResults = value;
                    OnPropertyChanged();
                }
            }
        }
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
        public ICommand RefreshCommand { get; }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (_isRefreshing == value) return;
                _isRefreshing = value;
                OnPropertyChanged();
                (RefreshCommand as Command)?.ChangeCanExecute();
            }
        }

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
                    OriginalDescription = "Quán ốc bình dân, đa dạng các loại ốc tươi ngon, nêm nếm đậm đà.",
                    Latitude = 10.7621,
                    Longitude = 106.7032
                },
                new FoodPlace
                {
                    Name = "Ốc Hồng Nhung",
                    Image = "ochongnhung.jpg",
                    Rating = 4.5,
                    OriginalDescription = "Nổi tiếng với các món ốc xào me, nướng mỡ hành thơm lừng.",
                    Latitude = 10.7615,
                    Longitude = 106.7038
                },
                new FoodPlace
                {
                    Name = "BONA Food and Beer",
                    Image = "bona.jpg",
                    Rating = 4.9,
                    OriginalDescription = "Kết hợp giữa đồ ăn ngon và bia tươi, cực kỳ sôi động về đêm.",
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
                    OriginalDescription = "Đồng giá 20k, phù hợp học sinh sinh viên, ngon và rẻ.",
                    Latitude = 10.7610,
                    Longitude = 106.7042
                },
                new FoodPlace
                {
                    Name = "Ốc Ty",
                    Image = "octy.jpg",
                    Rating = 4.7,
                    OriginalDescription = "Ốc tươi sống, phục vụ nhanh, không gian thoáng mát.",
                    Latitude = 10.7605,
                    Longitude = 106.7050
                },
                new FoodPlace
                {
                    Name = "Lãng Quán",
                    Image = "langquan.jpg",
                    Rating = 4.4,
                    OriginalDescription = "Hải sản tươi sống, không gian gia đình ấm cúng.",
                    Latitude = 10.7592,
                    Longitude = 106.7061
                }
            };

            RefreshTranslations();
            LocalizationResourceManager.Instance.PropertyChanged += (s, e) => RefreshTranslations();

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
            RefreshCommand = new Command(async () => await RefreshAsync());
        }

        private async Task RefreshAsync()
        {
            if (!await _refreshLock.WaitAsync(0)) return;

            try
            {
                // IsRefreshing is generally already set by the RefreshView, but if called manually:
                IsRefreshing = true;

                var syncTask = _poiService.SyncFromWebAsync(RenderSyncUrl);
                var completed = await Task.WhenAny(syncTask, Task.Delay(TimeSpan.FromSeconds(12)));

                if (completed == syncTask)
                {
                    await syncTask;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Refresh timeout. Using local cached data.");
                }

                // Await the heavy background load and the UI update
                await LoadAllRestaurantsAsync();

                // Adding a slight delay allows the native refresh animation to complete its final cycle, smoothing the stop UX
                await Task.Delay(150);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Refresh failed: {ex.Message}");
            }
            finally
            {
                // Pushing IsRefreshing = false via BeginInvokeOnMainThread ensures it executes AFTER the CollectionView's layout pass
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsRefreshing = false;
                });
                _refreshLock.Release();
            }
        }

        private async Task LoadAllRestaurantsAsync()
        {
            try
            {
                // Ensure service is initialized (triggers web sync on startup)
                await _poiService.InitializeAsync();

                var pois = await _poiService.GetAllPOIsAsync().ConfigureAwait(false);

                // Offload all string manipulations and list rebuilding to a background thread
                var processedList = await Task.Run(() =>
                {
                    var resultList = new List<FoodPlace>(pois.Count);

                    foreach (var poi in pois)
                    {
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
                            Image = !string.IsNullOrEmpty(poi.ImageUrl) ? poi.ImageUrl : imageName,
                            Rating = poi.Rating, // DB Rating
                            OriginalDescription = poi.Description ?? string.Empty,
                            Latitude = poi.Latitude,
                            Longitude = poi.Longitude,
                            PoiData = poi
                        };

                        resultList.Add(place);

                        // Note: It is safe to read memory on bg thread, we will not assign property observables here
                        var featured = FeaturedStalls.FirstOrDefault(f => f.Name == poi.Name);
                        if (featured != null)
                        {
                            featured.PoiData = poi;
                            featured.Latitude = poi.Latitude;
                            featured.Longitude = poi.Longitude;
                            featured.Rating = poi.Rating;
                            if (!string.IsNullOrEmpty(poi.ImageUrl)) featured.Image = poi.ImageUrl;
                        }

                        var popular = PopularSeafoodStalls.FirstOrDefault(p => p.Name == poi.Name);
                        if (popular != null)
                        {
                            popular.PoiData = poi;
                            popular.Latitude = poi.Latitude;
                            popular.Longitude = poi.Longitude;
                            popular.Rating = poi.Rating;
                            if (!string.IsNullOrEmpty(poi.ImageUrl)) popular.Image = poi.ImageUrl;
                        }
                    }

                    return resultList;
                });

                // Re-enter the main thread just once to update view models
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _allRestaurantsFullList = processedList;

                    RefreshTranslations(); // Updates translated descriptions

                    // Create new collections on the UI thread and reassign
                    AllRestaurants = new ObservableCollection<FoodPlace>(_allRestaurantsFullList);

                    FilterRestaurants();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading ALL POIs for Home: {ex.Message}");
            }
        }

        private void FilterRestaurants()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                IsSearching = false;
                SearchResults = new ObservableCollection<FoodPlace>();
                return;
            }

            IsSearching = true;
            var filtered = _allRestaurantsFullList
                .Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            SearchResults = new ObservableCollection<FoodPlace>(filtered);
        }

        private void RefreshTranslations()
        {
            foreach (var place in FeaturedStalls)
            {
                place.Description = LocalizationResourceManager.Instance[place.OriginalDescription];
            }
            foreach (var place in PopularSeafoodStalls)
            {
                place.Description = LocalizationResourceManager.Instance[place.OriginalDescription];
            }
            foreach (var place in AllRestaurants)
            {
                place.Description = LocalizationResourceManager.Instance[place.OriginalDescription];
            }
            foreach (var place in SearchResults)
            {
                place.Description = LocalizationResourceManager.Instance[place.OriginalDescription];
            }
            foreach (var place in _allRestaurantsFullList)
            {
                place.Description = LocalizationResourceManager.Instance[place.OriginalDescription];
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}