using SQLite;
using FoodStreetApp.Models;

namespace FoodStreetApp.Data
{
    public class POIRepository : IPOIRepository
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;

        public POIRepository()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "foodstreet.db3");
        }

        private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
        {
            if (_database == null)
            {
                _database = new SQLiteAsyncConnection(_dbPath);
                await _database.CreateTableAsync<POI>();
            }
            return _database;
        }

        // Increment this whenever seed data changes so all devices get the updated POIs on next launch.
        private const string SeedVersionKey = "poi_seed_version";
        private const int CurrentSeedVersion = 5; // v5: real GPS coordinates verified on Google Maps, Vinh Khanh Street District 4

        public async Task InitializeAsync()
        {
            var db = await GetDatabaseAsync();
            System.Diagnostics.Debug.WriteLine($"Database initialized at: {_dbPath}");

            var storedVersion = Preferences.Get(SeedVersionKey, 0);
            if (storedVersion < CurrentSeedVersion)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DB] Seed v{storedVersion} \u2192 v{CurrentSeedVersion} — wiping and re-seeding POIs");
                await db.DeleteAllAsync<POI>();
                await SeedDataAsync();
                Preferences.Set(SeedVersionKey, CurrentSeedVersion);
                return;
            }

            var count = await db.Table<POI>().CountAsync();
            if (count == 0)
            {
                await SeedDataAsync();
                Preferences.Set(SeedVersionKey, CurrentSeedVersion);
            }
        }

        public async Task<List<POI>> GetAllPOIsAsync()
        {
            var db = await GetDatabaseAsync();
            return await db.Table<POI>().ToListAsync();
        }

        public async Task<List<POI>> GetActivePOIsAsync()
        {
            var db = await GetDatabaseAsync();
            return await db.Table<POI>()
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Priority)
                .ToListAsync();
        }

        public async Task<POI?> GetPOIByIdAsync(int id)
        {
            var db = await GetDatabaseAsync();
            return await db.Table<POI>()
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<int> SavePOIAsync(POI poi)
        {
            var db = await GetDatabaseAsync();
            if (poi.Id != 0)
            {
                return await db.UpdateAsync(poi);
            }
            else
            {
                return await db.InsertAsync(poi);
            }
        }

        public async Task<int> UpdatePOIAsync(POI poi)
        {
            var db = await GetDatabaseAsync();
            return await db.UpdateAsync(poi);
        }

        public async Task<int> DeletePOIAsync(int id)
        {
            var db = await GetDatabaseAsync();
            return await db.DeleteAsync<POI>(id);
        }

        public async Task SeedDataAsync()
        {
            var db = await GetDatabaseAsync();

            // Real GPS coordinates verified on Google Maps, Vinh Khanh Food Street, District 4.
            // Route: Ốc Thảo → Lẩu gà lá é Con Gà Trống (south-east along Vinh Khanh St.)
            // Radius = 30 m (display circle); narration trigger uses GeofenceService.TriggerRadiusMeters.

            var samplePOIs = new List<POI>
            {
                // POI #1 - Ốc Thảo
                new POI
                {
                    Name = "Ốc Thảo",
                    Latitude = 10.761687,
                    Longitude = 106.702396,
                    Radius = 30,
                    ApproachRadius = 200,
                    Priority = 10,
                    Description = "Quán ốc tươi ngon trên đường Vĩnh Khánh",
                    TtsText = "Bạn đang đến gần Ốc Thảo, quán ốc tươi ngon trên đường Vĩnh Khánh.",
                    TtsTextEn = "You are approaching Oc Thao, a fresh snail restaurant on Vinh Khanh Street.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #2 - Ốc Vũ
                new POI
                {
                    Name = "Ốc Vũ",
                    Latitude = 10.761398,
                    Longitude = 106.702722,
                    Radius = 30,
                    ApproachRadius = 200,
                    Priority = 9,
                    Description = "Quán ốc đông khách với nhiều món chế biến đa dạng",
                    TtsText = "Ốc Vũ, quán ốc đông khách với nhiều món ốc chế biến đa dạng.",
                    TtsTextEn = "Oc Vu, a busy snail restaurant with many different preparations.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #3 - Ốc Oanh
                new POI
                {
                    Name = "Ốc Oanh",
                    Latitude = 10.760736,
                    Longitude = 106.703298,
                    Radius = 30,
                    ApproachRadius = 200,
                    Priority = 8,
                    Description = "Quán ốc nổi tiếng trên đường Vĩnh Khánh",
                    TtsText = "Bạn đang đến gần Ốc Oanh, một quán ốc nổi tiếng trên đường Vĩnh Khánh.",
                    TtsTextEn = "You are approaching Oc Oanh, a popular snail restaurant on Vinh Khanh Street.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #4 - A FAT HOT POT
                new POI
                {
                    Name = "A FAT HOT POT",
                    Latitude = 10.760600,
                    Longitude = 106.703528,
                    Radius = 30,
                    ApproachRadius = 200,
                    Priority = 7,
                    Description = "Nhà hàng lẩu đặc sắc trên đường Vĩnh Khánh",
                    TtsText = "Chào mừng bạn đến với A FAT HOT POT, nhà hàng lẩu đặc sắc.",
                    TtsTextEn = "Welcome to A FAT HOT POT, a distinctive hot pot restaurant.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #5 - Ốc Bụi
                new POI
                {
                    Name = "Ốc Bụi",
                    Latitude = 10.760615,
                    Longitude = 106.703932,
                    Radius = 30,
                    ApproachRadius = 200,
                    Priority = 6,
                    Description = "Quán ốc vỉa hè dân dã với nhiều món ngon",
                    TtsText = "Ốc Bụi, quán ốc vỉa hè dân dã với nhiều món ngon.",
                    TtsTextEn = "Oc Bui, a street-style snail eatery with many tasty dishes.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #6 - Ốc Biển Ngọc
                new POI
                {
                    Name = "Ốc Biển Ngọc",
                    Latitude = 10.7607448,
                    Longitude = 106.7044479,
                    Radius = 30,
                    ApproachRadius = 200,
                    Priority = 5,
                    Description = "Hải sản tươi ngon phong phú",
                    TtsText = "Ốc Biển Ngọc, nơi phục vụ hải sản tươi ngon phong phú.",
                    TtsTextEn = "Oc Bien Ngoc, serving a variety of fresh seafood.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #7 - Ốc Đào 2
                new POI
                {
                    Name = "Ốc Đào 2",
                    Latitude = 10.761180,
                    Longitude = 106.704961,
                    Radius = 30,
                    Priority = 4,
                    Description = "Quán ốc quen thuộc với nhiều món đặc trưng",
                    TtsText = "Ốc Đào 2, quán ốc quen thuộc với nhiều món ốc đặc trưng.",
                    TtsTextEn = "Oc Dao 2, a familiar snail restaurant with signature snail dishes.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #8 - Quán Ếch Thành Đạt
                new POI
                {
                    Name = "Quán Ếch Thành Đạt",
                    Latitude = 10.7613347,
                    Longitude = 106.705222,
                    Radius = 30,
                    Priority = 3,
                    Description = "Chuyên các món ếch độc đáo và ngon miệng",
                    TtsText = "Quán Ếch Thành Đạt, chuyên các món ếch độc đáo và ngon miệng.",
                    TtsTextEn = "Quan Ech Thanh Dat, specializing in unique and delicious frog dishes.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #9 - Ốc Diễm
                new POI
                {
                    Name = "Ốc Diễm",
                    Latitude = 10.761178,
                    Longitude = 106.706166,
                    Radius = 30,
                    Priority = 2,
                    Description = "Quán ốc tươi với không gian thoải mái",
                    TtsText = "Ốc Diễm, quán ốc tươi với không gian thoải mái.",
                    TtsTextEn = "Oc Diem, fresh snails in a relaxed atmosphere.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #10 - Lẩu gà lá é Con Gà Trống
                new POI
                {
                    Name = "Lẩu gà lá é Con Gà Trống",
                    Latitude = 10.760877,
                    Longitude = 106.706715,
                    Radius = 30,
                    Priority = 1,
                    Description = "Nhà hàng lẩu gà lá é đặc biệt",
                    TtsText = "Chào mừng bạn đến với Lẩu gà lá é Con Gà Trống, nhà hàng lẩu gà lá é đặc biệt.",
                    TtsTextEn = "Welcome to Lau Ga La E Con Ga Trong, featuring special lemon leaf chicken hot pot.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                }
            };

            foreach (var poi in samplePOIs)
            {
                await db.InsertAsync(poi);
            }

            System.Diagnostics.Debug.WriteLine($"Seeded {samplePOIs.Count} POIs to database (v{CurrentSeedVersion})");
        }
    }
}
