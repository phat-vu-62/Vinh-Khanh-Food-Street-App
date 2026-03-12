using FoodStreetApp.Models;
using FoodStreetApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FoodStreetApp.ViewModels
{
    public class POIListViewModel : INotifyPropertyChanged
    {
        private readonly IPOIService _poiService;
        private readonly INarrationService _narrationService;
        private bool _isLoading;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<POI> POIs { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ICommand ShowPOIDetailsCommand { get; }
        public ICommand PlayPreviewCommand { get; }

        public POIListViewModel(IPOIService poiService, INarrationService narrationService)
        {
            _poiService = poiService;
            _narrationService = narrationService;
            ShowPOIDetailsCommand = new Command<POI>(async (poi) => await ShowPOIDetails(poi));
            PlayPreviewCommand = new Command<POI>(async (poi) => await PlayPreview(poi));
        }

        public async Task LoadPOIsAsync()
        {
            try
            {
                IsLoading = true;
                var pois = await _poiService.GetAllPOIsAsync();

                POIs.Clear();
                foreach (var poi in pois.OrderByDescending(p => p.Priority).ThenBy(p => p.Name))
                {
                    POIs.Add(poi);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading POIs: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ShowPOIDetails(POI poi)
        {
            if (poi == null) return;

            var details = $"📍 Location: {poi.Latitude:F6}, {poi.Longitude:F6}\n" +
                         $"📏 Radius: {poi.Radius}m\n" +
                         $"⭐ Priority: {poi.Priority}\n" +
                         $"⏱️ Cooldown: {poi.CooldownSeconds}s\n" +
                         $"🎵 Audio: {(string.IsNullOrEmpty(poi.AudioFile) ? "TTS" : poi.AudioFile)}\n\n" +
                         $"{poi.Description}";

            await Application.Current!.MainPage!.DisplayAlert(
                poi.Name,
                details,
                "OK");
        }

        private async Task PlayPreview(POI poi)
        {
            if (poi == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Playing preview for: {poi.Name}");
                await _narrationService.PlayNarrationAsync(poi);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing preview: {ex.Message}");
                await Application.Current!.MainPage!.DisplayAlert(
                    "Playback Error",
                    "Unable to play audio preview. Check audio files and TTS settings.",
                    "OK");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
