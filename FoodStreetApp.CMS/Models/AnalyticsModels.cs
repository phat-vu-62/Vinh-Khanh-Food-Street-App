using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.CMS.Models;

public class AnalyticsSummary
{
    public int TotalAudioPlayed { get; set; }
    public int TotalPoiViewed { get; set; }
    public double AvgDurationSeconds { get; set; }
    public int UniqueUsersCount { get; set; }

    public List<TopPoiMetric> TopPois { get; set; } = new();
    public List<TrendPoint> DailyTrends { get; set; } = new();
    
    // Insights
    public string HotSpotName { get; set; } = "N/A";
    public string PeakHour { get; set; } = "N/A";
    public double EngagementRate { get; set; } // % of listen > 10s
    public int PendingApprovals { get; set; }
}

public class TopPoiMetric
{
    public int PoiId { get; set; }
    public string PoiName { get; set; } = "Unknown";
    public int Count { get; set; }

    // Language availability indicators
    public string? NameVi { get; set; }
    public string? NameEn { get; set; }
    public string? NameZh { get; set; }
    public string? NameKo { get; set; }
    public string? NameJa { get; set; }
}

public class TrendPoint
{
    public DateTime Date { get; set; }
    public int Views { get; set; }
    public int Listens { get; set; }
}
