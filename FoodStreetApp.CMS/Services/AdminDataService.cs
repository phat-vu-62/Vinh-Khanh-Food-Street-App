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
        item.ImageUrl = poi.ImageUrl; // Adding missing ImageUrl mapping

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

    public async Task<POI?> TranslatePoiAsync(int id)
    {
        var poi = _dbContext.Pois.FirstOrDefault(x => x.Id == id);
        if (poi is null) return null;

        bool isAiSuccess = await _translationService.TranslatePoiAsync(poi);
        
        _dbContext.SaveChanges();
        TrackPoiAction(poi.Id, "Updated");

        // We return the POI in both cases, but we might want the UI to know.
        // For simplicity, we stick to the POI return, but the service has already toasted if failed.
        return isAiSuccess ? poi : null; 
    }

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
                    bool success = await _translationService.TranslatePoiAsync(poi);
                    if (success)
                    {
                        _dbContext.SaveChanges();
                        TrackPoiAction(poi.Id, "Updated");
                        translatedCount++;
                        await Task.Delay(4000); // Rate limit for Gemini Free Tier (15 RPM -> 4s per item)
                    }
                    else 
                    {
                        // If it failed (likely 429), wait longer before trying the next POI
                        await Task.Delay(10000); 
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to batch translate POI {Id}.", poi.Id);
                }
            }
        }

        return translatedCount;
    }

    public async Task<FoodStreetApp.CMS.Models.AnalyticsSummary> GetAnalyticsSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _dbContext.UserHistories.AsNoTracking().AsQueryable();

        if (startDate.HasValue)
            query = query.Where(h => h.VisitedAtUtc >= startDate.Value.ToUniversalTime());
        if (endDate.HasValue)
            query = query.Where(h => h.VisitedAtUtc <= endDate.Value.ToUniversalTime());

        var logs = await query.ToListAsync();
        var pois = await _dbContext.Pois.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);

        var summary = new FoodStreetApp.CMS.Models.AnalyticsSummary
        {
            TotalAudioPlayed = logs.Count(l => (l.Action ?? "").Contains("audio", StringComparison.OrdinalIgnoreCase) || l.Action == "listen"),
            TotalPoiViewed = logs.Count(l => (l.Action ?? "").Contains("viewed", StringComparison.OrdinalIgnoreCase)),
            UniqueUsersCount = logs.Select(l => l.UserId).Distinct().Count(),
            AvgDurationSeconds = logs.Any(l => l.DurationSeconds.HasValue) ? logs.Where(l => l.DurationSeconds.HasValue).Average(l => l.DurationSeconds!.Value) : 0
        };

        // Top POIs
        summary.TopPois = logs.Where(l => pois.ContainsKey(l.PoiId))
            .GroupBy(l => l.PoiId)
            .Select(g => new FoodStreetApp.CMS.Models.TopPoiMetric
            {
                PoiId = g.Key,
                PoiName = pois[g.Key],
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        // Hot Spot
        summary.HotSpotName = summary.TopPois.FirstOrDefault()?.PoiName ?? "N/A";

        // Peak Hour
        if (logs.Any())
        {
            var peak = logs.GroupBy(l => l.VisitedAtUtc.ToLocalTime().Hour)
                .OrderByDescending(g => g.Count())
                .First();
            summary.PeakHour = $"{peak.Key:D2}:00 - {peak.Key + 1:D2}:00";
        }

        // Engagement Rate (> 10s)
        var audioLogs = logs.Where(l => (l.Action ?? "").Contains("audio", StringComparison.OrdinalIgnoreCase) || l.Action == "listen").ToList();
        if (audioLogs.Any())
        {
            summary.EngagementRate = (double)audioLogs.Count(l => l.DurationSeconds > 10) * 100 / audioLogs.Count;
        }

        // Daily Trends (Last 7 days if no range)
        var trendStart = startDate ?? DateTime.UtcNow.Date.AddDays(-6);
        var trendEnd = endDate ?? DateTime.UtcNow.Date;

        for (var d = trendStart.Date; d <= trendEnd.Date; d = d.AddDays(1))
        {
            summary.DailyTrends.Add(new FoodStreetApp.CMS.Models.TrendPoint
            {
                Date = d,
                Views = logs.Count(l => l.VisitedAtUtc.ToLocalTime().Date == d.Date && (l.Action ?? "").Contains("viewed", StringComparison.OrdinalIgnoreCase)),
                Listens = logs.Count(l => l.VisitedAtUtc.ToLocalTime().Date == d.Date && ((l.Action ?? "").Contains("audio", StringComparison.OrdinalIgnoreCase) || l.Action == "listen"))
            });
        }

        return summary;
    }
}

