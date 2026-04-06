using FoodStreetApp.CMS.Interfaces;
using FoodStreetApp.CMS.Data;
using FoodStreetApp.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodStreetApp.CMS.Services;

public class AdminDataService : IAdminDataService
{
    private readonly CmsDbContext _dbContext;
    private readonly IGeminiTranslationService _translationService;
    private readonly ILogger<AdminDataService> _logger;

    public AdminDataService(CmsDbContext dbContext, IGeminiTranslationService translationService, ILogger<AdminDataService> logger)
    {
        _dbContext = dbContext;
        _translationService = translationService;
        _logger = logger;
    }

    public IReadOnlyCollection<POI> GetPois() => _dbContext.Pois.OrderBy(x => x.Id).ToList();

    public POI? GetPoiById(int id) => _dbContext.Pois.FirstOrDefault(x => x.Id == id);

    public POI AddPoi(POI poi)
    {
        _dbContext.Pois.Add(poi);
        _dbContext.SaveChanges();
        TrackPoiAction(poi.Id, "Added");
        return poi;
    }

    public bool UpdatePoi(POI poi)
    {
        var item = _dbContext.Pois.FirstOrDefault(x => x.Id == poi.Id);
        if (item is null)
        {
            return false;
        }

        item.Name = poi.Name;
        item.Description = poi.Description;
        item.Latitude = poi.Latitude;
        item.Longitude = poi.Longitude;
        item.AudioUrl = poi.AudioUrl;
        item.IsActive = poi.IsActive;
        item.Type = poi.Type;
        item.RadiusMeters = poi.RadiusMeters;

        // Persist translation fields
        item.NameVi = poi.NameVi;
        item.NameEn = poi.NameEn;
        item.NameZh = poi.NameZh;
        item.NameKo = poi.NameKo;
        item.NameJa = poi.NameJa;
        item.DescriptionVi = poi.DescriptionVi;
        item.DescriptionEn = poi.DescriptionEn;
        item.DescriptionZh = poi.DescriptionZh;
        item.DescriptionKo = poi.DescriptionKo;
        item.DescriptionJa = poi.DescriptionJa;

        item.TextContent = poi.TextContent;
        item.TextContentVi = poi.TextContentVi;
        item.TextContentEn = poi.TextContentEn;
        item.TextContentZh = poi.TextContentZh;
        item.TextContentKo = poi.TextContentKo;
        item.TextContentJa = poi.TextContentJa;

        _dbContext.SaveChanges();
        TrackPoiAction(item.Id, "Updated");
        return true;
    }

    public bool DeletePoi(int id)
    {
        var item = _dbContext.Pois.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        var deletedId = item.Id;
        _dbContext.Pois.Remove(item);
        _dbContext.SaveChanges();
        TrackPoiAction(deletedId, "Deleted");
        return true;
    }

    private void TrackPoiAction(int poiId, string action)
    {
        _dbContext.PoiSyncActions.Add(new PoiSyncAction
        {
            PoiId = poiId,
            Action = action,
            OccurredAtUtc = DateTime.UtcNow
        });
        _dbContext.SaveChanges();
    }

    public IReadOnlyCollection<Audio> GetAudios() => _dbContext.Audios.OrderBy(x => x.Id).ToList();

    public Audio? GetAudioById(int id) => _dbContext.Audios.FirstOrDefault(x => x.Id == id);

    public Audio AddAudio(Audio audio)
    {
        _dbContext.Audios.Add(audio);
        _dbContext.SaveChanges();
        return audio;
    }

    public bool UpdateAudio(Audio audio)
    {
        var item = _dbContext.Audios.FirstOrDefault(x => x.Id == audio.Id);
        if (item is null)
        {
            return false;
        }

        item.PoiId = audio.PoiId;
        item.Title = audio.Title;
        item.Url = audio.Url;
        item.DurationSeconds = audio.DurationSeconds;
        item.IsActive = audio.IsActive;
        _dbContext.SaveChanges();
        return true;
    }

    public IReadOnlyCollection<UserHistory> GetUsageHistories() => _dbContext.UserHistories
        .AsNoTracking()
        .OrderByDescending(x => x.VisitedAtUtc)
        .ToList();

    public UserHistory AddUsageHistory(UserHistory history)
    {
        _dbContext.UserHistories.Add(history);
        _dbContext.SaveChanges();
        return history;
    }

    public bool DeleteAudio(int id)
    {
        var item = _dbContext.Audios.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        _dbContext.Audios.Remove(item);
        _dbContext.SaveChanges();
        return true;
    }

    public IReadOnlyCollection<Tour> GetTours() => _dbContext.Tours.AsNoTracking().OrderBy(x => x.Id).ToList();

    public Tour? GetTourById(int id) => _dbContext.Tours.FirstOrDefault(x => x.Id == id);

    public Tour AddTour(Tour tour)
    {
        _dbContext.Tours.Add(tour);
        _dbContext.SaveChanges();
        return tour;
    }

    public bool UpdateTour(Tour tour)
    {
        var item = _dbContext.Tours.FirstOrDefault(x => x.Id == tour.Id);
        if (item is null)
        {
            return false;
        }

        item.Name = tour.Name;
        item.Description = tour.Description;
        item.PoiIds = tour.PoiIds;
        item.IsActive = tour.IsActive;
        _dbContext.SaveChanges();
        return true;
    }

    public bool DeleteTour(int id)
    {
        var item = _dbContext.Tours.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        _dbContext.Tours.Remove(item);
        _dbContext.SaveChanges();
        return true;
    }

    public IReadOnlyCollection<Translation> GetTranslations() => _dbContext.Translations.OrderBy(x => x.Id).ToList();

    public Translation? GetTranslationById(int id) => _dbContext.Translations.FirstOrDefault(x => x.Id == id);

    public Translation AddTranslation(Translation translation)
    {
        _dbContext.Translations.Add(translation);
        _dbContext.SaveChanges();
        return translation;
    }

    public bool UpdateTranslation(Translation translation)
    {
        var item = _dbContext.Translations.FirstOrDefault(x => x.Id == translation.Id);
        if (item is null)
        {
            return false;
        }

        item.EntityName = translation.EntityName;
        item.EntityId = translation.EntityId;
        item.FieldName = translation.FieldName;
        item.Value = translation.Value;
        item.Language = translation.Language;
        _dbContext.SaveChanges();
        return true;
    }

    public bool DeleteTranslation(int id)
    {
        var item = _dbContext.Translations.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        _dbContext.Translations.Remove(item);
        _dbContext.SaveChanges();
        return true;
    }

    /// <summary>
    /// Translates a POI using the Gemini API and persists the translation fields.
    /// </summary>
    public async Task<POI?> TranslatePoiAsync(int id)
    {
        var poi = _dbContext.Pois.FirstOrDefault(x => x.Id == id);
        if (poi is null)
        {
            return null;
        }

        await _translationService.TranslatePoiAsync(poi);
        _dbContext.SaveChanges();
        TrackPoiAction(poi.Id, "Updated");

        _logger.LogInformation("POI {Id} translated and saved.", poi.Id);
        return poi;
    }

    /// <summary>
    /// Batch translates all POIs utilizing Gemini API locally and returns the number of objects modified.
    /// Skips any POI that already has English translation data.
    /// </summary>
    public async Task<int> TranslateAllPoisAsync()
    {
        var pois = _dbContext.Pois.ToList();
        int translatedCount = 0;

        foreach (var poi in pois)
        {
            if (string.IsNullOrWhiteSpace(poi.NameEn) || string.IsNullOrWhiteSpace(poi.DescriptionEn))
            {
                try
                {
                    await _translationService.TranslatePoiAsync(poi);
                    _dbContext.SaveChanges();
                    TrackPoiAction(poi.Id, "Updated");
                    
                    translatedCount++;
                    _logger.LogInformation("Batch translation successful for POI {Id}.", poi.Id);
                    
                    // Add delay to prevent hitting Gemini rate limits
                    await Task.Delay(300);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to batch translate POI {Id}.", poi.Id);
                }
            }
        }

        return translatedCount;
    }
}
