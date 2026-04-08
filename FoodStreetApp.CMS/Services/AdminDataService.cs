using FoodStreetApp.CMS.Interfaces;
using FoodStreetApp.Shared.Context;
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
        var start = startDate?.ToUniversalTime() ?? DateTime.UtcNow.Date.AddDays(-30).ToUniversalTime();
        var end = endDate?.ToUniversalTime() ?? DateTime.UtcNow.ToUniversalTime();

        var query = _dbContext.UserHistories.AsNoTracking()
            .Where(h => h.VisitedAtUtc >= start && h.VisitedAtUtc <= end);

        // 1. Basic KPIs in fewer roundtrips
        var stats = await query
            .GroupBy(h => 1)
            .Select(g => new
            {
                TotalAudio = g.Count(l => l.Action == "audio_played" || l.Action == "Listen"),
                TotalViews = g.Count(l => l.Action == "poi_viewed" || l.Action == "POI viewed"),
                HighEngagement = g.Count(l => (l.Action == "audio_played" || l.Action == "Listen") && l.DurationSeconds > 10),
                AvgDuration = g.Where(l => l.DurationSeconds.HasValue && l.DurationSeconds > 0).Average(l => (double?)l.DurationSeconds) ?? 0,
                UniqueUsers = g.Select(l => l.UserId).Distinct().Count()
            })
            .FirstOrDefaultAsync();

        if (stats == null) stats = new { TotalAudio = 0, TotalViews = 0, HighEngagement = 0, AvgDuration = 0d, UniqueUsers = 0 };

        // 2. Top POIs
        var topPoiData = await query
            .Where(l => l.Action == "qr_scanned" || l.Action == "poi_viewed" || l.Action == "POI viewed")
            .GroupBy(l => l.PoiId)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var poiIds = topPoiData.Select(x => x.Key).ToList();
        var poiNames = await _dbContext.Pois
            .Where(p => poiIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var topPois = topPoiData.Select(x => new FoodStreetApp.CMS.Models.TopPoiMetric
        {
            PoiId = x.Key,
            PoiName = poiNames.GetValueOrDefault(x.Key, $"POI #{x.Key}"),
            Count = x.Count
        }).ToList();

        // 3. Daily Trends (FIXED: Single query instead of loop)
        var trendData = await query
            .GroupBy(l => l.VisitedAtUtc.Date)
            .Select(g => new
            {
                Date = g.Key,
                Views = g.Count(l => l.Action == "poi_viewed" || l.Action == "POI viewed"),
                Listens = g.Count(l => l.Action == "audio_played" || l.Action == "Listen")
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var trends = trendData.Select(x => new FoodStreetApp.CMS.Models.TrendPoint
        {
            Date = x.Date,
            Views = x.Views,
            Listens = x.Listens
        }).ToList();

        // 4. Peak Hour
        var peakHourStr = "N/A";
        var peakGroup = await query
            .GroupBy(l => l.VisitedAtUtc.Hour)
            .OrderByDescending(g => g.Count())
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .FirstOrDefaultAsync();

        if (peakGroup != null)
        {
            peakHourStr = $"{peakGroup.Hour:D2}:00 - {peakGroup.Hour + 1:D2}:00";
        }

        return new FoodStreetApp.CMS.Models.AnalyticsSummary
        {
            TotalAudioPlayed = stats.TotalAudio,
            TotalPoiViewed = stats.TotalViews,
            UniqueUsersCount = stats.UniqueUsers,
            AvgDurationSeconds = stats.AvgDuration,
            TopPois = topPois,
            HotSpotName = topPois.FirstOrDefault()?.PoiName ?? "N/A",
            PeakHour = peakHourStr,
            EngagementRate = stats.TotalAudio > 0 ? (double)stats.HighEngagement * 100 / stats.TotalAudio : 0,
            DailyTrends = trends
        };
    }

    public async Task<(IReadOnlyCollection<UserHistory> Items, int TotalCount)> GetUsageHistoriesPagedAsync(int page, int pageSize, DateTime? date = null, int? poiId = null, string? search = null)
    {
        var query = _dbContext.UserHistories.AsNoTracking().AsQueryable();

        if (date.HasValue)
        {
            var utcStart = date.Value.Date.ToUniversalTime();
            var utcEnd = utcStart.AddDays(1);
            query = query.Where(h => h.VisitedAtUtc >= utcStart && h.VisitedAtUtc < utcEnd);
        }

        if (poiId.HasValue && poiId.Value > 0)
        {
            query = query.Where(h => h.PoiId == poiId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(h => h.UserId.Contains(search) || h.Action.Contains(search));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(h => h.VisitedAtUtc)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

        return (items, total);
    }
}


