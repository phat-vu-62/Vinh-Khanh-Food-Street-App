using System;
using Npgsql;

class Program
{
    static void Main()
    {
        var connString = "Host=dpg-d7igajnlk1mc739t25hg-a.singapore-postgres.render.com;Port=5432;Database=vinhkhanhdb;Username=vinhkhanhdb_user;Password=DyzxqOOxgXtys8KKt5rR6Bq49gg8Vfrm;SSL Mode=Require;Trust Server Certificate=true";

        using var conn = new NpgsqlConnection(connString);
        conn.Open();

        // Map POI Name => Owner Username => Owner GUID
        var mappings = new (string poiName, string ownerId)[]
        {
            ("Ốc Phát",                    "93d5fdc6-6d3e-41a2-b0cc-6031c863e7aa"),  // ocphat
            ("Ốc Thảo",                    "9ffb7e7d-8757-4d03-a331-353c10b186ac"),  // octhao
            ("Ốc Vũ",                      "b4ffd655-e9c3-4845-b23f-5925a73a3c9f"),  // ocvu
            ("Ốc Hồng Nhung",              "46886bf4-ec49-420b-a2b0-f82d996053c9"),  // ochongnhung
            ("Ốc Oanh",                    "0dd551ba-b375-4dd0-8a82-ce9b6bac3868"),  // ocoanh
            ("A FAT HOT POT",              "2de0e929-fb64-431f-b867-8be86169a4cd"),  // afathotpot
            ("Ốc Bụi",                     "a88c4832-d03f-482c-bd31-06e09f742470"),  // ocbui
            ("Win - Trà Sữa - Coffee",     "e2217587-21b9-448a-912f-49a6f0a3465b"),  // wintrasuacoffee
            ("BONA Food and Beer",         "1a51d831-7881-4ce7-a6b2-017e06433ade"),  // bonafoodandbeer
            ("Ốc Đào 2",                   "deadf3f4-747d-4320-a7d6-c42dd35cee39"),  // ocdao2
            ("Lãng Quán",                  "64a88a9b-d607-4bc2-b072-1d43edbc3c3d"),  // langquan
            ("Ốc Nhi 20k",                "b212a83a-276b-4edb-acc4-d3243b96bdd6"),  // ocnhi20k
            ("Ốc Diễm",                    "2f38225f-1c21-44a0-903b-346e4ebeca89"),  // ocdiem
            ("Lẩu gà lá é Con Gà Trống",  "0404a6ee-a848-48e7-89a2-7ace17350df4"),  // laugalaecongatrong
            ("Ốc Ty",                      "692766b9-b9bc-4e17-b099-01c0434dfffa"),  // octy
        };

        Console.WriteLine("=== ASSIGNING OWNER IDs ===");
        foreach (var (poiName, ownerId) in mappings)
        {
            using var cmd = new NpgsqlCommand(
                "UPDATE \"Pois\" SET \"OwnerId\" = @ownerId WHERE \"Name\" = @name AND \"OwnerId\" IS NULL;", conn);
            cmd.Parameters.AddWithValue("ownerId", Guid.Parse(ownerId));
            cmd.Parameters.AddWithValue("name", poiName);
            var rows = cmd.ExecuteNonQuery();
            Console.WriteLine($"  {poiName} => {ownerId.Substring(0, 8)}... ({rows} row updated)");
        }

        // Verify
        Console.WriteLine("\n=== VERIFICATION ===");
        using (var cmd = new NpgsqlCommand(
            "SELECT p.\"Name\", u.\"Username\" FROM \"Pois\" p LEFT JOIN \"Users\" u ON p.\"OwnerId\" = u.\"Id\" ORDER BY p.\"Id\";", conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var owner = reader.IsDBNull(1) ? "UNASSIGNED" : reader.GetString(1);
                Console.WriteLine($"  {reader.GetString(0)} => {owner}");
            }
        }

        Console.WriteLine("\nDone!");
    }
}
