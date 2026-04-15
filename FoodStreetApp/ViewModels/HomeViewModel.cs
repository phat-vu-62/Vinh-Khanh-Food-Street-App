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
            FeaturedStalls = new ObservableCollection<FoodPlace>();
            PopularSeafoodStalls = new ObservableCollection<FoodPlace>();

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

                // Offload all mapping and filtering to a background thread for UI fluidness
                var (allList, featuredList, seafoodList) = await Task.Run(() =>
                {
                    FoodPlace MapPoi(POI poi)
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

                        return new FoodPlace
                        {
                            Name = poi.Name,
                            Image = !string.IsNullOrEmpty(poi.ImageUrl) ? poi.ImageUrl : imageName,
                            Rating = poi.Rating,
                            OriginalDescription = poi.Description ?? string.Empty,
                            Latitude = poi.Latitude,
                            Longitude = poi.Longitude,
                            PoiData = poi
                        };
                    }

                    // 1. Build All Restaurants
                    var all = pois.Select(MapPoi).ToList();

                    // 2. Build Featured (Top 3 by priority)
                    var featured = pois.OrderByDescending(p => p.Priority)
                                      .Take(3)
                                      .Select(MapPoi)
                                      .ToList();

                    // 3. Build Seafood Section (Contains "Ốc" or "Hải sản")
                    var seafood = pois.Where(p => p.Name.Contains("Ốc", StringComparison.OrdinalIgnoreCase) || 
                                                 p.Name.Contains("Seafood", StringComparison.OrdinalIgnoreCase))
                                     .OrderByDescending(p => p.Rating)
                                     .Take(6)
                                     .Select(MapPoi)
                                     .ToList();

                    return (all, featured, seafood);
                });
 
                 // Re-enter the main thread just once to update view models
                 await MainThread.InvokeOnMainThreadAsync(() =>
                 {
                    _allRestaurantsFullList = allList;
 
                    // Update dynamic collections
                    FeaturedStalls.Clear();
                    foreach (var p in featuredList) FeaturedStalls.Add(p);

                    PopularSeafoodStalls.Clear();
                    foreach (var p in seafoodList) PopularSeafoodStalls.Add(p);

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