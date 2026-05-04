using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.CMS.Models;

// ============================================================
// EXISTING (kept for backward compat with SilentRefresh)
// ============================================================

public class AnalyticsSummary
{
    public int TotalAudioPlayed { get; set; }
    public int TotalPoiViewed { get; set; }
    public double AvgDurationSeconds { get; set; }
    public int UniqueUsersCount { get; set; }
    public int ActiveUsersNow { get; set; } // App users active in last 5 seconds
    public int ActiveQrUsersNow { get; set; } // QR listeners active in last 5 seconds

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

// ============================================================
// NEW: Admin Dashboard
// ============================================================

public class AdminDashboardData
{
    // KPI Cards
    public int TotalPois { get; set; }
    public int TotalQrScans { get; set; }
    public int TotalUniqueUsers { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ActiveUsersNow { get; set; }
    public int ActiveQrUsersNow { get; set; }
    public int PendingApprovals { get; set; }

    // Revenue breakdown
    public decimal RevenueQr { get; set; }
    public decimal RevenueCreatePoi { get; set; }
    public decimal RevenueRefund { get; set; }

    // Growth Analytics — Line chart (30d)
    public List<TrendPoint> DailyActivity { get; set; } = new();

    // Performance Ranking
    public List<PoiRankMetric> Top10Pois { get; set; } = new();
    public List<PoiRankMetric> Bottom5Pois { get; set; } = new();

    // Revenue
    public List<RevenueTrendPoint> RevenueByDay { get; set; } = new();
    public List<PoiRevenueMetric> RevenueByPoi { get; set; } = new();

    // User Behavior
    public double AvgScansPerUser { get; set; }
    public int NewUsersLast7d { get; set; }
    public int ReturningUsersLast7d { get; set; }
    public List<HourlyActivity> PeakHours { get; set; } = new();

    // Alerts
    public List<string> ZeroScanPois { get; set; } = new();
}

// ============================================================
// NEW: Owner Dashboard
// ============================================================

public class OwnerDashboardData
{
    // KPI Cards
    public int MyPoiCount { get; set; }
    public int TotalScans { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingApprovals { get; set; }

    // POI Performance table
    public List<OwnerPoiPerformance> PoiPerformance { get; set; } = new();

    // Time-based
    public List<TrendPoint> DailyScans { get; set; } = new();
    public List<HourlyActivity> PeakHours { get; set; } = new();

    // Alerts
    public List<string> NoEngagementPois { get; set; } = new();
}

// ============================================================
// Supporting DTOs
// ============================================================

public class PoiRankMetric
{
    public int PoiId { get; set; }
    public string PoiName { get; set; } = "";
    public int ScanCount { get; set; }
    public int ViewCount { get; set; }
    public int ListenCount { get; set; }
    public decimal Revenue { get; set; }
}

public class RevenueTrendPoint
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public class HourlyActivity
{
    public int Hour { get; set; }
    public int Count { get; set; }
}

public class OwnerPoiPerformance
{
    public int PoiId { get; set; }
    public string PoiName { get; set; } = "";
    public int ScanCount { get; set; }
    public decimal Revenue { get; set; }
    public bool IsApproved { get; set; }
}

public class PoiRevenueMetric
{
    public string PoiName { get; set; } = "";
    public decimal Revenue { get; set; }
}
