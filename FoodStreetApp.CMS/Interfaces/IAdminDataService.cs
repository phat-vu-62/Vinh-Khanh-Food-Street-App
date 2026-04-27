using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.CMS.Interfaces;

public interface IAdminDataService
{
    IReadOnlyCollection<POI> GetPois();
    Task<IReadOnlyCollection<POI>> GetPoisAsync();
    IReadOnlyCollection<POI> GetPoisByOwnerId(Guid ownerId);

    POI? GetPoiById(int id);
    POI AddPoi(POI poi);
    bool UpdatePoi(POI poi);
    bool DeletePoi(int id);
    Task<bool> ApprovePoiAsync(int id);
    Task<bool> UnapprovePoiAsync(int id);


    IReadOnlyCollection<Audio> GetAudios();
    Audio? GetAudioById(int id);
    Audio AddAudio(Audio audio);
    bool UpdateAudio(Audio audio);
    bool DeleteAudio(int id);

    IReadOnlyCollection<Tour> GetTours();
    Tour? GetTourById(int id);
    Tour AddTour(Tour tour);
    bool UpdateTour(Tour tour);
    bool DeleteTour(int id);

    IReadOnlyCollection<Translation> GetTranslations();
    Translation? GetTranslationById(int id);
    Translation AddTranslation(Translation translation);
    bool UpdateTranslation(Translation translation);
    bool DeleteTranslation(int id);

    IReadOnlyCollection<UserHistory> GetUsageHistories();
    UserHistory AddUsageHistory(UserHistory history);

    /// <summary>
    /// Translates a POI using Gemini API and persists the translations.
    /// </summary>
    Task<POI?> TranslatePoiAsync(int id);
    
    /// <summary>
    /// Translates all POIs that are missing English translation and returns the total successfully translated count.
    /// </summary>
    Task<int> TranslateAllPoisAsync();

    /// <summary>
    /// Gets aggregated analytics metrics for the specified date range.
    /// </summary>
    Task<FoodStreetApp.CMS.Models.AnalyticsSummary> GetAnalyticsSummaryAsync(DateTime? startDate = null, DateTime? endDate = null, Guid? ownerId = null);

    /// <summary>
    /// Full admin analytics dashboard data (charts, rankings, revenue, user behavior).
    /// </summary>
    Task<FoodStreetApp.CMS.Models.AdminDashboardData> GetAdminDashboardAsync();

    /// <summary>
    /// Lightweight live metrics for 5s auto-refresh (online users, scan counts).
    /// </summary>
    Task<(int ActiveUsers, int ActiveQr, int TotalScans, int UniqueUsers)> GetLiveMetricsAsync();

    /// <summary>
    /// Heatmap data filtered by date: returns list of (PoiId, PoiName, Lat, Lng, ScanCount).
    /// </summary>
    Task<List<(int PoiId, string PoiName, double Lat, double Lng, int ScanCount)>> GetHeatmapDataAsync(DateTime? date);

    /// <summary>
    /// Full owner analytics dashboard data filtered to the owner's POIs.
    /// </summary>
    Task<FoodStreetApp.CMS.Models.OwnerDashboardData> GetOwnerDashboardAsync(Guid ownerId);


    /// <summary>
    /// Gets a paged list of usage histories with optional filtering.
    /// </summary>
    Task<(IReadOnlyCollection<UserHistory> Items, int TotalCount)> GetUsageHistoriesPagedAsync(int page, int pageSize, DateTime? date = null, int? poiId = null, string? search = null, string? source = null);

    /// <summary>
    /// Gets audio_played and poi_viewed counts for a specific POI.
    /// </summary>
    Task<(int AudioCount, int ViewCount, int AppCount, int QrCount)> GetPoiActionStatsAsync(int poiId, DateTime? date = null);

    /// <summary>
    /// Returns set of (DeviceId, PoiId) pairs that came from QR scans.
    /// </summary>
    Task<HashSet<(string DeviceId, int PoiId)>> GetQrDevicePoiPairsAsync();
}


