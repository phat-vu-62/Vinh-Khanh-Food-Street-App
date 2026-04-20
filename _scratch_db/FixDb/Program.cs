using System;
using Npgsql;

class Program
{
    static void Main()
    {
        var connString = "Host=dpg-d7igajnlk1mc739t25hg-a.singapore-postgres.render.com;Port=5432;Database=vinhkhanhdb;Username=vinhkhanhdb_user;Password=DyzxqOOxgXtys8KKt5rR6Bq49gg8Vfrm;SSL Mode=Require;Trust Server Certificate=true";

        using var conn = new NpgsqlConnection(connString);
        conn.Open();
        Console.WriteLine("Connected to database!");

        // 1. Check existing tables
        Console.WriteLine("\n=== EXISTING TABLES ===");
        using (var cmd = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name;", conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
                Console.WriteLine($"  - {reader.GetString(0)}");
        }

        // 2. Check POIs count
        Console.WriteLine("\n=== DATA COUNTS ===");
        string[] tables = { "Pois", "Audios", "Tours", "Translations", "UserHistories", "Users", "PoiSyncActions" };
        foreach (var table in tables)
        {
            try
            {
                using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{table}\";", conn);
                var count = cmd.ExecuteScalar();
                Console.WriteLine($"  {table}: {count} rows");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {table}: ERROR - {ex.Message}");
            }
        }

        // 3. Check columns on Pois table
        Console.WriteLine("\n=== Pois COLUMNS ===");
        using (var cmd = new NpgsqlCommand("SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'Pois' ORDER BY ordinal_position;", conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
                Console.WriteLine($"  {reader.GetString(0)} ({reader.GetString(1)})");
        }

        // 4. Add Priority column if missing
        Console.WriteLine("\n=== ADDING PRIORITY COLUMN ===");
        using (var cmd = new NpgsqlCommand("ALTER TABLE \"Pois\" ADD COLUMN IF NOT EXISTS \"Priority\" integer NOT NULL DEFAULT 0;", conn))
        {
            cmd.ExecuteNonQuery();
            Console.WriteLine("  Priority column ensured!");
        }

        Console.WriteLine("\nDone!");
    }
}
