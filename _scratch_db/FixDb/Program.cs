using System;
using Npgsql;

class Program
{
    static void Main()
    {
        var connString = "Host=dpg-d7igajnlk1mc739t25hg-a.singapore-postgres.render.com;Port=5432;Database=vinhkhanhdb;Username=vinhkhanhdb_user;Password=DyzxqOOxgXtys8KKt5rR6Bq49gg8Vfrm;SSL Mode=Require;Trust Server Certificate=true";

        using var conn = new NpgsqlConnection(connString);
        conn.Open();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");

        // 1. What does the dashboard query return?
        Console.WriteLine("=== UNIQUE USERIDs (last 30d, excluding pings) ===");
        using (var cmd = new NpgsqlCommand($@"
            SELECT DISTINCT ""UserId""
            FROM ""UserHistories""
            WHERE ""VisitedAtUtc"" >= '{thirtyDaysAgo}'
            AND ""Action"" NOT IN ('app_ping', 'cms_ping', 'qr_listen_ping')
            ORDER BY ""UserId""
        ", conn))
        using (var reader = cmd.ExecuteReader())
        {
            int count = 0;
            while (reader.Read())
            {
                count++;
                Console.WriteLine($"  {count}. {reader.GetString(0)}");
            }
            Console.WriteLine($"\n  TOTAL: {count}");
        }

        // 2. Break down by Action type
        Console.WriteLine("\n=== ACTION BREAKDOWN (last 30d, exclude pings) ===");
        using (var cmd = new NpgsqlCommand($@"
            SELECT ""Action"", COUNT(DISTINCT ""UserId"") as unique_users, COUNT(*) as total_events
            FROM ""UserHistories""
            WHERE ""VisitedAtUtc"" >= '{thirtyDaysAgo}'
            AND ""Action"" NOT IN ('app_ping', 'cms_ping', 'qr_listen_ping')
            GROUP BY ""Action""
            ORDER BY unique_users DESC
        ", conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
                Console.WriteLine($"  {reader.GetString(0),-25} | Users: {reader.GetInt64(1),-5} | Events: {reader.GetInt64(2)}");
        }

        Console.WriteLine("\nDone!");
    }
}
