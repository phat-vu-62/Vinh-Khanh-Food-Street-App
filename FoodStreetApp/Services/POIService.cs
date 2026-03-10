using FoodStreetApp.Models;
using FoodStreetApp.Data;

namespace FoodStreetApp.Services
{
    public class POIService : IPOIService
    {
        private readonly IPOIRepository _repository;
        private List<POI> _cachedPOIs = new();

        public POIService(IPOIRepository repository)
        {
            _repository = repository;
        }

        public async Task InitializeAsync()
        {
            await _repository.InitializeAsync();
            _cachedPOIs = await _repository.GetActivePOIsAsync();
            System.Diagnostics.Debug.WriteLine($"POIService initialized with {_cachedPOIs.Count} active POIs");
        }

        public async Task<List<POI>> GetAllPOIsAsync()
        {
            if (_cachedPOIs.Count == 0)
            {
                _cachedPOIs = await _repository.GetAllPOIsAsync();
            }
            return _cachedPOIs;
        }

        public async Task<List<POI>> GetActivePOIsAsync()
        {
            if (_cachedPOIs.Count == 0)
            {
                _cachedPOIs = await _repository.GetActivePOIsAsync();
            }
            return _cachedPOIs.Where(p => p.IsActive).ToList();
        }

        public void UpdatePOIStatus(POI poi)
        {
            var cachedPoi = _cachedPOIs.FirstOrDefault(p => p.Id == poi.Id);
            if (cachedPoi != null)
            {
                cachedPoi.HasPlayed = poi.HasPlayed;
                cachedPoi.LastTriggered = poi.LastTriggered;
            }
        }

        public void ResetAllPOIStatus()
        {
            foreach (var poi in _cachedPOIs)
            {
                poi.HasPlayed = false;
                poi.LastTriggered = null;
            }
            System.Diagnostics.Debug.WriteLine("All POI statuses reset");
        }
    }
}
