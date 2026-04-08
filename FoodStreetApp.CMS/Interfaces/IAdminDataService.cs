using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.CMS.Interfaces;

public interface IAdminDataService
{
    IReadOnlyCollection<POI> GetPois();
    POI? GetPoiById(int id);
    POI AddPoi(POI poi);
    bool UpdatePoi(POI poi);
    bool DeletePoi(int id);

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
    Task<FoodStreetApp.CMS.Models.AnalyticsSummary> GetAnalyticsSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
}

