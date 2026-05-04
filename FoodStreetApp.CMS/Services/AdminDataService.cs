using FoodStreetApp.CMS.Interfaces;
using FoodStreetApp.Shared.Context;
using FoodStreetApp.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodStreetApp.CMS.Services;

public class AdminDataService : IAdminDataService
{
    private readonly CmsDbContext _dbContext;
    private readonly IDbContextFactory<CmsDbContext> _dbFactory;
    private readonly IGeminiTranslationService _translationService;
    private readonly ILogger<AdminDataService> _logger;

    public AdminDataService(CmsDbContext dbContext, IDbContextFactory<CmsDbContext> dbFactory, IGeminiTranslationService translationService, ILogger<AdminDataService> logger)
    {
        _dbContext = dbContext;
        _dbFactory = dbFactory;
        _translationService = translationService;
        _logger = logger;
    }

    public IReadOnlyCollection<POI> GetPois() => _dbContext.Pois.OrderBy(x => x.Id).ToList();
    public async Task<IReadOnlyCollection<POI>> GetPoisAsync() 
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Pois.OrderBy(x => x.Id).ToListAsync();
    }
    public IReadOnlyCollection<POI> GetPoisByOwnerId(Guid ownerId) => _dbContext.Pois.Where(x => x.OwnerId == ownerId).OrderBy(x => x.Id).ToList();


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
        item.IsApproved = poi.IsApproved;
        item.Type = poi.Type;

        item.RadiusMeters = poi.RadiusMeters;
        item.Priority = poi.Priority;
        item.ImageUrl = poi.ImageUrl;
        item.OwnerId = poi.OwnerId;

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
        var wasPending = !item.IsApproved;
        var ownerId = item.OwnerId;
        
        // Ensure it was NEVER approved in the past before refunding
        var everApproved = _dbContext.PoiSyncActions.Any(x => x.PoiId == deletedId && x.Action == "Approved");

        _dbContext.Pois.Remove(item);

        // If the POI was never approved, refund the 200k
        if (wasPending && !everApproved && ownerId != Guid.Empty)
        {
            _dbContext.UserHistories.Add(new UserHistory 
            {
                DeviceId = ownerId.ToString(),
                PoiId = deletedId,
                Action = "refund_create_poi",
                Amount = -200000,
                VisitedAtUtc = DateTime.UtcNow
            });
        }

        _dbContext.SaveChanges();
        TrackPoiAction(deletedId, "Deleted");
        return true;
    }

    public async Task<bool> ApprovePoiAsync(int id)
    {
        var item = await _dbContext.Pois.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return false;

        item.IsApproved = true;
        item.IsActive = true; // Automatically activate when approved
        await _dbContext.SaveChangesAsync();
        TrackPoiAction(item.Id, "Approved");
        return true;
    }

    public async Task<bool> UnapprovePoiAsync(int id)
    {
        var item = await _dbContext.Pois.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return false;

        item.IsApproved = false;
        item.IsActive = false; // Automatically deactivate when unapproved
        await _dbContext.SaveChangesAsync();
        TrackPoiAction(item.Id, "Unapproved");
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

    public async Task<FoodStreetApp.CMS.Models.AnalyticsSummary> GetAnalyticsSummaryAsync(DateTime? startDate = null, DateTime? endDate = null, Guid? ownerId = null)
    {
        // Use a dedicated DbContext to avoid concurrency issues with the 2s dashboard timer
        await using var db = await _dbFactory.CreateDbContextAsync();

        var start = startDate?.ToUniversalTime() ?? DateTime.UtcNow.Date.AddDays(-30).ToUniversalTime();
        var end = endDate?.ToUniversalTime() ?? DateTime.UtcNow.ToUniversalTime();

        var query = db.UserHistories.AsNoTracking()
            .Where(h => h.VisitedAtUtc >= start && h.VisitedAtUtc <= end);

        // If ownerId is provided, filter history by the owner's POIs
        if (ownerId.HasValue)
        {
            var myPoiIds = await db.Pois
                .Where(p => p.OwnerId == ownerId.Value)
                .Select(p => p.Id)
                .ToListAsync();
            
            query = query.Where(h => myPoiIds.Contains(h.PoiId));
        }

        // 1. Basic KPIs in fewer roundtrips

        var stats = await query
            .GroupBy(h => 1)
            .Select(g => new
            {
                TotalAudio = g.Count(l => l.Action == "audio_played" || l.Action == "Listen"),
                TotalViews = g.Count(l => l.Action == "poi_viewed" || l.Action == "POI viewed" || l.Action == "qr_scanned" || l.Action == "Route_Entry"),
                HighEngagement = g.Count(l => (l.Action == "audio_played" || l.Action == "Listen") && l.DurationSeconds > 10),
                AvgDuration = g.Where(l => l.DurationSeconds.HasValue && l.DurationSeconds > 0).Average(l => (double?)l.DurationSeconds) ?? 0,
                UniqueUsers = g.Select(l => l.DeviceId).Distinct().Count()
            })
            .FirstOrDefaultAsync();

        if (stats == null) stats = new { TotalAudio = 0, TotalViews = 0, HighEngagement = 0, AvgDuration = 0d, UniqueUsers = 0 };

        // 2. Top POIs
        var topPoiData = await (from h in query
                                join p in db.Pois on h.PoiId equals p.Id // Skip deleted POIs
                                where (h.Action == "qr_scanned" || h.Action == "poi_viewed" || h.Action == "POI viewed"
                                    || h.Action == "audio_played" || h.Action == "Listen")
                                group h by h.PoiId into g
                                orderby g.Count() descending
                                select new { Key = g.Key, Count = g.Count() })
                                .Take(5)
                                .ToListAsync();

        var poiIds = topPoiData.Select(x => x.Key).ToList();
        var poiDetails = await db.Pois
            .Where(p => poiIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var topPois = topPoiData.Select(x => {
            var p = poiDetails.GetValueOrDefault(x.Key);
            return new FoodStreetApp.CMS.Models.TopPoiMetric
            {
                PoiId = x.Key,
                PoiName = p?.Name ?? $"POI #{x.Key}",
                Count = x.Count,
                NameVi = p?.NameVi,
                NameEn = p?.NameEn,
                NameZh = p?.NameZh,
                NameKo = p?.NameKo,
                NameJa = p?.NameJa
            };
        }).ToList();

        // 3. Daily Trends (FIXED: Single query instead of loop)
        var trendData = await query
            .GroupBy(l => l.VisitedAtUtc.Date)
            .Select(g => new
            {
                Date = g.Key,
                Views = g.Count(l => l.Action == "poi_viewed" || l.Action == "POI viewed" || l.Action == "qr_scanned" || l.Action == "Route_Entry"),
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

        // 5. Active Users Now (last 4 seconds)
        var recentThreshold = DateTime.UtcNow.AddSeconds(-4);
        var activeNow = await db.UserHistories.AsNoTracking()
            .Where(h => h.VisitedAtUtc >= recentThreshold && (h.Action == "app_ping" || h.Action == "cms_ping"))
            .Select(h => h.DeviceId)
            .Distinct()
            .CountAsync();

        // 5b. Active QR Listeners (last 4 seconds)
        var activeQrNow = await db.UserHistories.AsNoTracking()
            .Where(h => h.VisitedAtUtc >= recentThreshold && h.Action == "qr_listen_ping")
            .Select(h => h.DeviceId)
            .Distinct()
            .CountAsync();

        return new FoodStreetApp.CMS.Models.AnalyticsSummary
        {
            TotalAudioPlayed = stats.TotalAudio,
            TotalPoiViewed = stats.TotalViews,
            UniqueUsersCount = stats.UniqueUsers,
            ActiveUsersNow = activeNow,
            ActiveQrUsersNow = activeQrNow,
            AvgDurationSeconds = stats.AvgDuration,
            TopPois = topPois,
            HotSpotName = topPois.FirstOrDefault()?.PoiName ?? "N/A",
            PeakHour = peakHourStr,
            EngagementRate = stats.TotalAudio > 0 ? (double)stats.HighEngagement * 100 / stats.TotalAudio : 0,
            DailyTrends = trends,
            PendingApprovals = await db.Pois.CountAsync(p => !p.IsApproved && (!ownerId.HasValue || p.OwnerId == ownerId.Value))
        };


    }

    public async Task<(IReadOnlyCollection<UserHistory> Items, int TotalCount)> GetUsageHistoriesPagedAsync(int page, int pageSize, DateTime? date = null, int? poiId = null, string? search = null, string? source = null)
    {
        var query = _dbContext.UserHistories.AsNoTracking().AsQueryable();

        // Only show audio/narration actions
        var allowedActions = new[] { "audio_played", "poi_viewed" };
        query = query.Where(h => allowedActions.Contains(h.Action));

        // Hide records from deleted POIs
        var existingPoiIds = _dbContext.Pois.Select(p => p.Id);
        query = query.Where(h => existingPoiIds.Contains(h.PoiId));

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

        // Filter by source: check if DeviceId+PoiId has a qr_scanned record
        if (source == "qr")
        {
            var qrPairs = _dbContext.UserHistories.AsNoTracking()
                .Where(h => h.Action == "qr_scanned")
                .Select(h => new { h.DeviceId, h.PoiId }).Distinct();
            query = query.Where(h => qrPairs.Any(q => q.DeviceId == h.DeviceId && q.PoiId == h.PoiId));
        }
        else if (source == "app")
        {
            var qrPairs = _dbContext.UserHistories.AsNoTracking()
                .Where(h => h.Action == "qr_scanned")
                .Select(h => new { h.DeviceId, h.PoiId }).Distinct();
            query = query.Where(h => !qrPairs.Any(q => q.DeviceId == h.DeviceId && q.PoiId == h.PoiId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(h => h.DeviceId.Contains(search) || h.Action.Contains(search));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(h => h.VisitedAtUtc)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

        return (items, total);
    }

    public async Task<(int AudioCount, int ViewCount, int AppCount, int QrCount)> GetPoiActionStatsAsync(int poiId, DateTime? date = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.UserHistories.AsNoTracking().AsQueryable();

        var allowedActions = new[] { "audio_played", "poi_viewed" };
        query = query.Where(h => allowedActions.Contains(h.Action));

        // Hide records from deleted POIs (same as list query)
        var existingPoiIds = db.Pois.Select(p => p.Id);
        query = query.Where(h => existingPoiIds.Contains(h.PoiId));

        if (poiId > 0)
            query = query.Where(h => h.PoiId == poiId);

        if (date.HasValue)
        {
            var utcStart = date.Value.Date.ToUniversalTime();
            var utcEnd = utcStart.AddDays(1);
            query = query.Where(h => h.VisitedAtUtc >= utcStart && h.VisitedAtUtc < utcEnd);
        }

        var audioCount = await query.CountAsync(h => h.Action == "audio_played");
        var viewCount = await query.CountAsync(h => h.Action == "poi_viewed");

        // QR = records where same DeviceId+PoiId has a qr_scanned entry
        var qrPairs = db.UserHistories.AsNoTracking()
            .Where(h => h.Action == "qr_scanned")
            .Select(h => new { h.DeviceId, h.PoiId }).Distinct();
        var qrCount = await query.CountAsync(h => qrPairs.Any(q => q.DeviceId == h.DeviceId && q.PoiId == h.PoiId));
        var appCount = (audioCount + viewCount) - qrCount;

        return (audioCount, viewCount, appCount, qrCount);
    }

    /// <summary>
    /// Returns set of (DeviceId, PoiId) pairs that have qr_scanned records, for UI display.
    /// </summary>
    public async Task<HashSet<(string DeviceId, int PoiId)>> GetQrDevicePoiPairsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var pairs = await db.UserHistories.AsNoTracking()
            .Where(h => h.Action == "qr_scanned")
            .Select(h => new { h.DeviceId, h.PoiId })
            .Distinct()
            .ToListAsync();
        return pairs.Select(p => (p.DeviceId, p.PoiId)).ToHashSet();
    }

    // ================================================================
    // NEW: Full Admin Dashboard
    // ================================================================
    private static readonly string[] InteractionActions = { "qr_scanned", "poi_viewed", "POI viewed", "audio_played", "Listen", "Route_Entry" };
    private static readonly string[] ScanActions = { "qr_scanned", "poi_viewed", "POI viewed", "Route_Entry" };
    private static readonly string[] RevenueActions = { "payment_listen", "register_merchant", "payment_create_poi" };

    /// <summary>
    /// Heatmap data optionally filtered by a specific date. Returns POI coordinates + scan counts.
    /// </summary>
    public async Task<List<(int PoiId, string PoiName, double Lat, double Lng, int ScanCount)>> GetHeatmapDataAsync(DateTime? date)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var validActions = new[] { "audio_played", "poi_viewed" };
        IQueryable<UserHistory> query = db.UserHistories.AsNoTracking()
            .Where(h => validActions.Contains(h.Action));

        if (date.HasValue)
        {
            // Convert Vietnam date to UTC range
            var vnDate = date.Value.Date;
            var utcStart = vnDate.AddHours(-7); // 00:00 VN = 17:00 UTC (day before)
            var utcEnd = utcStart.AddDays(1);
            query = query.Where(h => h.VisitedAtUtc >= utcStart && h.VisitedAtUtc < utcEnd);
        }
        else
        {
            // Default: last 30 days
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            query = query.Where(h => h.VisitedAtUtc >= thirtyDaysAgo);
        }

        var poiScans = await query
            .GroupBy(h => h.PoiId)
            .Select(g => new { PoiId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToListAsync();

        var pois = await db.Pois.AsNoTracking().ToListAsync();
        var result = new List<(int PoiId, string PoiName, double Lat, double Lng, int ScanCount)>();

        foreach (var ps in poiScans)
        {
            var poi = pois.FirstOrDefault(p => p.Id == ps.PoiId);
            if (poi != null && poi.Latitude != 0 && poi.Longitude != 0)
            {
                result.Add((poi.Id, poi.Name ?? $"POI #{poi.Id}", poi.Latitude, poi.Longitude, ps.Count));
            }
        }

        return result;
    }

    /// <summary>
    /// Lightweight query for 5s auto-refresh — only fetches live metrics.
    /// </summary>
    public async Task<(int ActiveUsers, int ActiveQr, int TotalScans, int UniqueUsers)> GetLiveMetricsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var recentThreshold = DateTime.UtcNow.AddSeconds(-4);
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var activeUsers = (await db.UserHistories.AsNoTracking()
            .Where(h => h.VisitedAtUtc >= recentThreshold && (h.Action == "app_ping" || h.Action == "cms_ping"))
            .Select(h => h.DeviceId).Distinct().CountAsync());
        var activeQr = (await db.UserHistories.AsNoTracking()
            .Where(h => h.VisitedAtUtc >= recentThreshold && h.Action == "qr_listen_ping")
            .Select(h => h.DeviceId).Distinct().CountAsync());
        var liveValidActions = new[] { "audio_played", "poi_viewed" };
        var liveExistingPoiIds = db.Pois.Select(p => p.Id);
        var totalScans = await db.UserHistories.AsNoTracking()
            .Where(h => h.VisitedAtUtc >= thirtyDaysAgo && liveValidActions.Contains(h.Action) && liveExistingPoiIds.Contains(h.PoiId))
            .CountAsync();
        var uniqueUsers = await db.UserHistories.AsNoTracking()
            .Where(h => h.VisitedAtUtc >= thirtyDaysAgo && h.Action != "app_ping" && h.Action != "cms_ping" && h.Action != "qr_listen_ping")
            .Select(h => h.DeviceId).Distinct().CountAsync();

        return (activeUsers, activeQr, totalScans, uniqueUsers);
    }

    public async Task<FoodStreetApp.CMS.Models.AdminDashboardData> GetAdminDashboardAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var sevenDaysAgo = now.AddDays(-7);

        var result = new FoodStreetApp.CMS.Models.AdminDashboardData();

        // ── KPI Cards ──
        result.TotalPois = await db.Pois.CountAsync(p => p.IsApproved);
        result.PendingApprovals = await db.Pois.CountAsync(p => !p.IsApproved);

        var last30 = db.UserHistories.AsNoTracking().Where(h => h.VisitedAtUtc >= thirtyDaysAgo);

        // Lượt truy cập = only audio_played + poi_viewed from existing POIs
        var validActions = new[] { "audio_played", "poi_viewed" };
        var existingPoiIds = db.Pois.Select(p => p.Id);
        result.TotalQrScans = await last30.CountAsync(h => validActions.Contains(h.Action) && existingPoiIds.Contains(h.PoiId));
        result.TotalUniqueUsers = await last30
            .Where(h => h.Action != "app_ping" && h.Action != "cms_ping" && h.Action != "qr_listen_ping")
            .Select(h => h.DeviceId).Distinct().CountAsync();

        // Active users now
        var recentThreshold = now.AddSeconds(-4);
        result.ActiveUsersNow = (await db.UserHistories.AsNoTracking()
            .Where(h => h.VisitedAtUtc >= recentThreshold && (h.Action == "app_ping" || h.Action == "cms_ping"))
            .Select(h => h.DeviceId).Distinct().CountAsync());
        result.ActiveQrUsersNow = (await db.UserHistories.AsNoTracking()
            .Where(h => h.VisitedAtUtc >= recentThreshold && h.Action == "qr_listen_ping")
            .Select(h => h.DeviceId).Distinct().CountAsync());

        // ── Revenue (matching /api/auth/revenue logic) ──
        var allHist = db.UserHistories.AsNoTracking();
        var registerRev = await allHist.Where(h => h.Action == "register_merchant" && h.Amount > 0).SumAsync(h => h.Amount ?? 0);
        var listenRev = await allHist.Where(h => h.Action == "payment_listen" && h.Amount > 0).SumAsync(h => h.Amount ?? 0);
        var createPoiGross = await allHist.Where(h => h.Action == "payment_create_poi" && h.Amount > 0).SumAsync(h => h.Amount ?? 0);
        result.RevenueRefund = Math.Abs(await allHist.Where(h => h.Amount < 0).SumAsync(h => h.Amount ?? 0));
        result.RevenueQr = listenRev;
        result.RevenueCreatePoi = createPoiGross;
        result.TotalRevenue = registerRev + listenRev + createPoiGross - result.RevenueRefund;

        // ── Growth Analytics: Daily Activity (30d) ──
        var chartValidActions = new[] { "audio_played", "poi_viewed" };
        var chartExistingPoiIds = db.Pois.Select(p => p.Id);
        var dailyRaw = await last30
            .Where(h => chartValidActions.Contains(h.Action) && chartExistingPoiIds.Contains(h.PoiId))
            .Select(h => new { h.VisitedAtUtc.Date, h.Action })
            .ToListAsync();

        result.DailyActivity = dailyRaw
            .GroupBy(h => h.Date)
            .Select(g => new FoodStreetApp.CMS.Models.TrendPoint
            {
                Date = g.Key,
                Views = g.Count(x => x.Action == "poi_viewed"),
                Listens = g.Count(x => x.Action == "audio_played")
            })
            .OrderBy(x => x.Date)
            .ToList();

        // ── Top 10 POIs by scans ──
        var topRaw = await (from h in last30
                             join p in db.Pois on h.PoiId equals p.Id
                             where chartValidActions.Contains(h.Action)
                             select new { h.PoiId, p.Name, h.Action, h.Amount })
                             .ToListAsync();

        result.Top10Pois = topRaw
            .GroupBy(x => new { x.PoiId, x.Name })
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new FoodStreetApp.CMS.Models.PoiRankMetric
            {
                PoiId = g.Key.PoiId,
                PoiName = g.Key.Name,
                ScanCount = g.Count(),
                ViewCount = g.Count(x => x.Action == "poi_viewed"),
                ListenCount = g.Count(x => x.Action == "audio_played"),
                Revenue = g.Sum(x => x.Amount ?? 0)
            })
            .ToList();

        // ── Bottom 5 POIs (approved, fewest scans) ──
        var allPoiIds = await db.Pois.Where(p => p.IsApproved).Select(p => new { p.Id, p.Name }).ToListAsync();
        var scanCounts = await last30
            .Where(h => chartValidActions.Contains(h.Action))
            .GroupBy(h => h.PoiId)
            .Select(g => new { PoiId = g.Key, Count = g.Count() })
            .ToListAsync();
        var scanDict = scanCounts.ToDictionary(x => x.PoiId, x => x.Count);
        result.Bottom5Pois = allPoiIds
            .Select(p => new FoodStreetApp.CMS.Models.PoiRankMetric
            {
                PoiId = p.Id,
                PoiName = p.Name,
                ScanCount = scanDict.GetValueOrDefault(p.Id, 0)
            })
            .OrderBy(x => x.ScanCount)
            .Take(5)
            .ToList();

        // ── Revenue by Day (30d) ──
        result.RevenueByDay = await db.UserHistories.AsNoTracking()
            .Where(h => h.VisitedAtUtc >= thirtyDaysAgo && RevenueActions.Contains(h.Action) && h.Amount > 0)
            .GroupBy(h => h.VisitedAtUtc.Date)
            .Select(g => new FoodStreetApp.CMS.Models.RevenueTrendPoint
            {
                Date = g.Key,
                Amount = g.Sum(x => x.Amount ?? 0)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // ── Revenue by POI (Doughnut) ──
        result.RevenueByPoi = await (from h in db.UserHistories.AsNoTracking()
                                     join p in db.Pois on h.PoiId equals p.Id
                                     where RevenueActions.Contains(h.Action) && h.Amount > 0
                                     group h by p.Name into g
                                     orderby g.Sum(x => x.Amount ?? 0) descending
                                     select new FoodStreetApp.CMS.Models.PoiRevenueMetric
                                     {
                                         PoiName = g.Key,
                                         Revenue = g.Sum(x => x.Amount ?? 0)
                                     })
                                     .Take(8)
                                     .ToListAsync();

        // ── User Behavior ──
        var interactionUsers = await last30
            .Where(h => InteractionActions.Contains(h.Action))
            .ToListAsync();

        var totalInteractions = interactionUsers.Count;
        var distinctUsers = interactionUsers.Select(h => h.DeviceId).Distinct().Count();
        result.AvgScansPerUser = distinctUsers > 0 ? (double)totalInteractions / distinctUsers : 0;

        // New vs Returning (last 7 days)
        var last7dUsers = await db.UserHistories.AsNoTracking()
            .Where(h => h.VisitedAtUtc >= sevenDaysAgo && InteractionActions.Contains(h.Action))
            .Select(h => h.DeviceId).Distinct().ToListAsync();
        var usersBeforeLast7d = await db.UserHistories.AsNoTracking()
            .Where(h => h.VisitedAtUtc < sevenDaysAgo && InteractionActions.Contains(h.Action))
            .Select(h => h.DeviceId).Distinct().ToListAsync();
        var returningSet = new HashSet<string>(usersBeforeLast7d);
        result.ReturningUsersLast7d = last7dUsers.Count(u => returningSet.Contains(u));
        result.NewUsersLast7d = last7dUsers.Count - result.ReturningUsersLast7d;

        // ── Peak Hours (UTC → VN time +7) ──
        var peakRaw = await last30
            .Where(h => chartValidActions.Contains(h.Action) && chartExistingPoiIds.Contains(h.PoiId))
            .Select(h => h.VisitedAtUtc.Hour)
            .ToListAsync();

        result.PeakHours = peakRaw
            .GroupBy(utcHour => (utcHour + 7) % 24)
            .Select(g => new FoodStreetApp.CMS.Models.HourlyActivity { Hour = g.Key, Count = g.Count() })
            .ToList();
        // Fill missing hours with 0
        var peakDict = result.PeakHours.ToDictionary(x => x.Hour, x => x.Count);
        result.PeakHours = Enumerable.Range(0, 24)
            .Select(h => new FoodStreetApp.CMS.Models.HourlyActivity { Hour = h, Count = peakDict.GetValueOrDefault(h, 0) })
            .ToList();

        // ── Alerts: POIs with zero scans ──
        result.ZeroScanPois = allPoiIds
            .Where(p => !scanDict.ContainsKey(p.Id) || scanDict[p.Id] == 0)
            .Select(p => p.Name)
            .ToList();

        return result;
    }

    // ================================================================
    // NEW: Full Owner Dashboard
    // ================================================================

    public async Task<FoodStreetApp.CMS.Models.OwnerDashboardData> GetOwnerDashboardAsync(Guid ownerId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var fourteenDaysAgo = now.AddDays(-14);

        var result = new FoodStreetApp.CMS.Models.OwnerDashboardData();

        // Get owner's POIs
        var myPois = await db.Pois.Where(p => p.OwnerId == ownerId).ToListAsync();
        var myPoiIds = myPois.Select(p => p.Id).ToList();

        result.MyPoiCount = myPois.Count;
        result.PendingApprovals = myPois.Count(p => !p.IsApproved);

        if (myPoiIds.Count == 0) return result;

        var myHistory = db.UserHistories.AsNoTracking()
            .Where(h => myPoiIds.Contains(h.PoiId));

        // ── KPIs ──
        result.TotalScans = await myHistory.CountAsync(h => ScanActions.Contains(h.Action));
        result.TotalRevenue = await myHistory
            .Where(h => RevenueActions.Contains(h.Action) && h.Amount > 0)
            .SumAsync(h => h.Amount ?? 0);

        // ── POI Performance table ──
        var scansByPoi = await myHistory
            .Where(h => ScanActions.Contains(h.Action))
            .GroupBy(h => h.PoiId)
            .Select(g => new { PoiId = g.Key, Count = g.Count() })
            .ToListAsync();
        var revByPoi = await myHistory
            .Where(h => RevenueActions.Contains(h.Action) && h.Amount > 0)
            .GroupBy(h => h.PoiId)
            .Select(g => new { PoiId = g.Key, Rev = g.Sum(x => x.Amount ?? 0) })
            .ToListAsync();
        var scanMap = scansByPoi.ToDictionary(x => x.PoiId, x => x.Count);
        var revMap = revByPoi.ToDictionary(x => x.PoiId, x => x.Rev);
        result.PoiPerformance = myPois.Select(p => new FoodStreetApp.CMS.Models.OwnerPoiPerformance
        {
            PoiId = p.Id,
            PoiName = p.Name,
            ScanCount = scanMap.GetValueOrDefault(p.Id, 0),
            Revenue = revMap.GetValueOrDefault(p.Id, 0),
            IsApproved = p.IsApproved
        }).OrderByDescending(x => x.ScanCount).ToList();

        // ── Daily Scans (14d) ──
        result.DailyScans = await myHistory
            .Where(h => h.VisitedAtUtc >= fourteenDaysAgo && ScanActions.Contains(h.Action))
            .GroupBy(h => h.VisitedAtUtc.Date)
            .Select(g => new FoodStreetApp.CMS.Models.TrendPoint
            {
                Date = g.Key,
                Views = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // ── Peak Hours ──
        result.PeakHours = await myHistory
            .Where(h => InteractionActions.Contains(h.Action))
            .GroupBy(h => h.VisitedAtUtc.Hour)
            .Select(g => new FoodStreetApp.CMS.Models.HourlyActivity { Hour = g.Key, Count = g.Count() })
            .OrderBy(x => x.Hour)
            .ToListAsync();
        var peakDict = result.PeakHours.ToDictionary(x => x.Hour, x => x.Count);
        result.PeakHours = Enumerable.Range(0, 24)
            .Select(h => new FoodStreetApp.CMS.Models.HourlyActivity { Hour = h, Count = peakDict.GetValueOrDefault(h, 0) })
            .ToList();

        // ── Alerts ──
        result.NoEngagementPois = result.PoiPerformance
            .Where(p => p.ScanCount == 0)
            .Select(p => p.PoiName)
            .ToList();

        return result;
    }
}


