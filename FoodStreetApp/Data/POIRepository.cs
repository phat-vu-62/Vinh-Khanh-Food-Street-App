using SQLite;
using FoodStreetApp.Models;
using System.Net.Http.Json;

namespace FoodStreetApp.Data
{
    public class POIRepository : IPOIRepository
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;
        private readonly SemaphoreSlim _initLock = new(1, 1);

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
        private const string HasSyncedWithWebKey = "has_synced_with_web";
        private const int CurrentSeedVersion = 11; // v11: Fixed duplication issue with thread-safe init
        private const string LastSyncUtcKey = "poi_last_sync_utc";

        public async Task InitializeAsync()
        {
            await _initLock.WaitAsync();
            try
            {
                var db = await GetDatabaseAsync();
                System.Diagnostics.Debug.WriteLine($"Database initialized at: {_dbPath}");

                // IF WE ALREADY SYNCED WITH WEB AT LEAST ONCE, WE NEVER SEED LOCAL DATA AGAIN.
                if (Preferences.Get(HasSyncedWithWebKey, false))
                {
                    System.Diagnostics.Debug.WriteLine("[DB] Skipping seed because app has already synced with Web.");
                    return;
                }

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
            finally
            {
                _initLock.Release();
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

        public async Task<int> SyncFromWebAsync(string? syncUrl = null)
        {
            const string defaultUrl = "https://vinh-khanh-food-street-app.onrender.com/api/sync/pois";
            var targetUrl = string.IsNullOrWhiteSpace(syncUrl) ? defaultUrl : syncUrl;
            var actionsUrl = targetUrl.Replace("/api/sync/pois", "/api/sync/poi-actions", StringComparison.OrdinalIgnoreCase);

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(20)
            };

            var db = await GetDatabaseAsync();
            var lastSyncUtc = Preferences.Get(LastSyncUtcKey, string.Empty);

            if (!string.IsNullOrWhiteSpace(lastSyncUtc))
            {
                try
                {
                    var actionRequestUrl = $"{actionsUrl}?sinceUtc={Uri.EscapeDataString(lastSyncUtc)}";
                    var actions = await httpClient.GetFromJsonAsync<List<RemotePoiActionDto>>(actionRequestUrl);
                    if (actions is not null)
                    {
                        var affected = 0;
                        foreach (var action in actions.OrderBy(x => x.OccurredAtUtc))
                        {
                            if (string.Equals(action.Action, "Deleted", StringComparison.OrdinalIgnoreCase))
                            {
                                affected += await db.DeleteAsync<POI>(action.PoiId);
                                continue;
                            }

                            if (action.Poi is null)
                            {
                                continue;
                            }

                            var localPoi = MapRemotePoi(action.Poi);
                            var existing = await db.Table<POI>().Where(x => x.Id == localPoi.Id).FirstOrDefaultAsync();
                            if (existing is null)
                            {
                                // Check by name too to prevent duplicates if IDs were shifted - but only if it's not a fresh sync
                                var byName = await db.Table<POI>().Where(x => x.Name == localPoi.Name).FirstOrDefaultAsync();
                                if (byName != null)
                                {
                                    // If names match but IDs don't, delete the old local "Seed" version and insert the new web version
                                    await db.DeleteAsync(byName);
                                }
                                affected += await db.InsertAsync(localPoi);
                            }
                            else
                            {
                                affected += await db.UpdateAsync(localPoi);
                            }
                        }

                        Preferences.Set(LastSyncUtcKey, DateTime.UtcNow.ToString("O"));
                        return affected;
                    }
                }
                catch
                {
                    // Fallback to full sync when action endpoint is unavailable.
                }
            }

            // FULL SYNC FALLBACK OR FIRST TIME SYNC
            try
            {
                var remotePois = await httpClient.GetFromJsonAsync<List<RemotePoiDto>>(targetUrl);
                if (remotePois is null || remotePois.Count == 0)
                {
                    return 0;
                }

                var alreadySyncedOnce = Preferences.Get(HasSyncedWithWebKey, false);

                // IF THIS IS OUR FIRST SUCCESSFUL CONNECTION TO WEB, WIPE ALL SEEDED DATA TO PREVENT DUPLICATES
                if (!alreadySyncedOnce)
                {
                    System.Diagnostics.Debug.WriteLine("[SYNC] First web sync ever — wiping local database to remove duplicates.");
                    await db.DeleteAllAsync<POI>();
                    Preferences.Set(HasSyncedWithWebKey, true);
                }
                else
                {
                    // Regular full sync wipe
                    await db.DeleteAllAsync<POI>();
                }

                var localPois = remotePois.Select(MapRemotePoi).ToList();
                await db.InsertAllAsync(localPois);
                Preferences.Set(LastSyncUtcKey, DateTime.UtcNow.ToString("O"));
                return localPois.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SYNC] Error during web sync: {ex.Message}");
                return 0;
            }
        }

        private static POI MapRemotePoi(RemotePoiDto p)
        {
            return new POI
            {
                Id = p.Id,
                Name = p.Name ?? string.Empty,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Radius = p.Radius <= 0 ? 15 : p.Radius,
                ApproachRadius = p.ApproachRadius <= 0 ? 200 : p.ApproachRadius,
                Priority = p.Priority <= 0 ? p.Id : p.Priority,
                Rating = p.Rating <= 0 ? 4.5 : p.Rating,
                ReviewCount = p.ReviewCount < 0 ? 0 : p.ReviewCount,
                Description = p.Description ?? string.Empty,
                AudioFile = p.AudioFile ?? string.Empty,
                TtsText = p.TtsText ?? p.Description ?? p.Name ?? string.Empty,
                TtsTextEn = p.TtsTextEn ?? string.Empty,
                TtsTextKo = p.TtsTextKo ?? string.Empty,
                TtsTextZh = p.TtsTextZh ?? string.Empty,
                TtsTextJa = p.TtsTextJa ?? string.Empty,
                UseTts = p.UseTts,
                CooldownSeconds = p.CooldownSeconds <= 0 ? 60 : p.CooldownSeconds,
                IsActive = p.IsActive,
                ImageUrl = p.ImageUrl ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task SeedDataAsync()
        {
            var db = await GetDatabaseAsync();

            // Real GPS coordinates verified on Google Maps, Vinh Khanh Food Street, District 4.
            // Route: Ốc Phát → Ốc Ty (west to east along Vinh Khanh St.)
            // Radius = 15 m (display circle); narration trigger uses GeofenceService.TriggerRadiusMeters.

            var samplePOIs = new List<POI>
            {
                // POI #1 - Ốc Phát
                new POI
                {
                    Name = "Ốc Phát",
                    Latitude = 10.761943,
                    Longitude = 106.702050,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 16,
                    Rating = 4.8,
                    ReviewCount = 250,
                    Description = "Quán ốc tươi ngon đầu đường Vĩnh Khánh",
                    TtsText = "Bạn đang đến gần Ốc Phát, quán ốc tươi ngon đầu đường Vĩnh Khánh.",
                    TtsTextEn = "You are approaching Oc Phat, a fresh snail restaurant at the start of Vinh Khanh Street.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #2 - Ốc Thảo
                new POI
                {
                    Name = "Ốc Thảo",
                    Latitude = 10.761687,
                    Longitude = 106.702396,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 15,
                    Rating = 4.3,
                    ReviewCount = 180,
                    Description = "Quán ốc tươi ngon trên đường Vĩnh Khánh",
                    TtsText = "Bạn đang đến gần Ốc Thảo, quán ốc tươi ngon trên đường Vĩnh Khánh.",
                    TtsTextEn = "You are approaching Oc Thao, a fresh snail restaurant on Vinh Khanh Street.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #3 - Ốc Vũ
                new POI
                {
                    Name = "Ốc Vũ",
                    Latitude = 10.761398,
                    Longitude = 106.702722,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 14,
                    Rating = 4.2,
                    ReviewCount = 120,
                    Description = "Quán ốc đông khách với nhiều món chế biến đa dạng",
                    TtsText = "Ốc Vũ, quán ốc đông khách với nhiều món ốc chế biến đa dạng.",
                    TtsTextEn = "Oc Vu, a busy snail restaurant with many different preparations.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #4 - Ốc Hồng Nhung
                new POI
                {
                    Name = "Ốc Hồng Nhung",
                    Latitude = 10.761153,
                    Longitude = 106.703071,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 13,
                    Rating = 4.5,
                    ReviewCount = 310,
                    Description = "Quán ốc nổi tiếng trên đường Vĩnh Khánh",
                    TtsText = "Ốc Hồng Nhung, quán ốc nổi tiếng trên đường Vĩnh Khánh.",
                    TtsTextEn = "Oc Hong Nhung, a well-known snail restaurant on Vinh Khanh Street.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #5 - Ốc Oanh
                new POI
                {
                    Name = "Ốc Oanh",
                    Latitude = 10.760736,
                    Longitude = 106.703298,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 12,
                    Rating = 4.0,
                    ReviewCount = 450,
                    Description = "Quán ốc nổi tiếng trên đường Vĩnh Khánh",
                    TtsText = "Bạn đang đến gần Ốc Oanh, một quán ốc nổi tiếng trên đường Vĩnh Khánh.",
                    TtsTextEn = "You are approaching Oc Oanh, a popular snail restaurant on Vinh Khanh Street.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #6 - A FAT HOT POT
                new POI
                {
                    Name = "A FAT HOT POT",
                    Latitude = 10.760600,
                    Longitude = 106.703528,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 11,
                    Rating = 4.5,
                    ReviewCount = 150,
                    Description = "Nhà hàng lẩu đặc sắc trên đường Vĩnh Khánh",
                    TtsText = "Chào mừng bạn đến với A FAT HOT POT, nhà hàng lẩu đặc sắc.",
                    TtsTextEn = "Welcome to A FAT HOT POT, a distinctive hot pot restaurant.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #7 - Ốc Bụi
                new POI
                {
                    Name = "Ốc Bụi",
                    Latitude = 10.760615,
                    Longitude = 106.703932,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 10,
                    Rating = 4.7,
                    ReviewCount = 200,
                    Description = "Quán ốc vỉa hè dân dã với nhiều món ngon",
                    TtsText = "Ốc Bụi, quán ốc vỉa hè dân dã với nhiều món ngon.",
                    TtsTextEn = "Oc Bui, a street-style snail eatery with many tasty dishes.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #8 - Win - Trà Sữa - Coffee
                new POI
                {
                    Name = "Win - Trà Sữa - Coffee",
                    Latitude = 10.760705,
                    Longitude = 106.704130,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 9,
                    Rating = 4.6,
                    ReviewCount = 110,
                    Description = "Điểm dừng chân giải khát trà sữa và cà phê",
                    TtsText = "Win Trà Sữa Coffee, điểm dừng chân giải khát trên đường Vĩnh Khánh.",
                    TtsTextEn = "Win Tea and Coffee, a refreshment stop on Vinh Khanh Street.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #9 - BONA Food and Beer
                new POI
                {
                    Name = "BONA Food and Beer",
                    Latitude = 10.760761,
                    Longitude = 106.704660,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 8,
                    Rating = 4.9,
                    ReviewCount = 500,
                    Description = "Nhà hàng bia và đồ ăn trên đường Vĩnh Khánh",
                    TtsText = "Chào mừng bạn đến với BONA Food and Beer, nhà hàng bia và đồ ăn trên đường Vĩnh Khánh.",
                    TtsTextEn = "Welcome to BONA Food and Beer, a food and beer restaurant on Vinh Khanh Street.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #10 - Ốc Đào 2
                new POI
                {
                    Name = "Ốc Đào 2",
                    Latitude = 10.761180,
                    Longitude = 106.704961,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 7,
                    Rating = 4.1,
                    ReviewCount = 220,
                    Description = "Quán ốc quen thuộc với nhiều món đặc trưng",
                    TtsText = "Ốc Đào 2, quán ốc quen thuộc với nhiều món ốc đặc trưng.",
                    TtsTextEn = "Oc Dao 2, a familiar snail restaurant with signature snail dishes.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #11 - Lãng Quán
                new POI
                {
                    Name = "Lãng Quán",
                    Latitude = 10.761127,
                    Longitude = 106.705453,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 6,
                    Rating = 4.4,
                    ReviewCount = 180,
                    Description = "Quán ăn đặc biệt trên đường Vĩnh Khánh",
                    TtsText = "Lãng Quán, quán ăn đặc biệt trên đường Vĩnh Khánh.",
                    TtsTextEn = "Lang Quan, a special restaurant on Vinh Khanh Street.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #12 - Ốc Nhi 20k
                new POI
                {
                    Name = "Ốc Nhi 20k",
                    Latitude = 10.761299,
                    Longitude = 106.705973,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 5,
                    Rating = 4.6,
                    ReviewCount = 340,
                    Description = "Quán ốc giá rẻ với nhiều món ngon",
                    TtsText = "Ốc Nhi 20k, quán ốc giá rẻ với nhiều món ngon trên đường Vĩnh Khánh.",
                    TtsTextEn = "Oc Nhi 20k, a budget-friendly snail restaurant with many tasty dishes.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #13 - Ốc Diễm
                new POI
                {
                    Name = "Ốc Diễm",
                    Latitude = 10.761178,
                    Longitude = 106.706166,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 4,
                    Rating = 4.2,
                    ReviewCount = 160,
                    Description = "Quán ốc tươi với không gian thoải mái",
                    TtsText = "Ốc Diễm, quán ốc tươi với không gian thoải mái.",
                    TtsTextEn = "Oc Diem, fresh snails in a relaxed atmosphere.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #14 - Lẩu gà lá é Con Gà Trống
                new POI
                {
                    Name = "Lẩu gà lá é Con Gà Trống",
                    Latitude = 10.760877,
                    Longitude = 106.706715,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 3,
                    Rating = 4.3,
                    ReviewCount = 210,
                    Description = "Nhà hàng lẩu gà lá é đặc biệt",
                    TtsText = "Chào mừng bạn đến với Lẩu gà lá é Con Gà Trống, nhà hàng lẩu gà lá é đặc biệt.",
                    TtsTextEn = "Welcome to Lau Ga La E Con Ga Trong, featuring special lemon leaf chicken hot pot.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #15 - Ốc Ty
                new POI
                {
                    Name = "Ốc Ty",
                    Latitude = 10.760725,
                    Longitude = 106.706940,
                    Radius = 15,
                    ApproachRadius = 200,
                    Priority = 2,
                    Rating = 4.7,
                    ReviewCount = 290,
                    Description = "Quán ốc cuối đường Vĩnh Khánh",
                    TtsText = "Bạn đang đến gần Ốc Ty, quán ốc cuối đường Vĩnh Khánh.",
                    TtsTextEn = "You are approaching Oc Ty, a snail restaurant at the end of Vinh Khanh Street.",
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

        private class RemotePoiDto
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Radius { get; set; }
            public double ApproachRadius { get; set; }
            public int Priority { get; set; }
            public double Rating { get; set; }
            public int ReviewCount { get; set; }
            public string? Description { get; set; }
            public string? AudioFile { get; set; }
            public string? TtsText { get; set; }
            public string? TtsTextEn { get; set; }
            public string? TtsTextKo { get; set; }
            public string? TtsTextZh { get; set; }
            public string? TtsTextJa { get; set; }
            public bool UseTts { get; set; }
            public int CooldownSeconds { get; set; }
            public bool IsActive { get; set; }
            public string? ImageUrl { get; set; }
        }

        private class RemotePoiActionDto
        {
            public int PoiId { get; set; }
            public string Action { get; set; } = string.Empty;
            public DateTime OccurredAtUtc { get; set; }
            public RemotePoiDto? Poi { get; set; }
        }
    }
}
