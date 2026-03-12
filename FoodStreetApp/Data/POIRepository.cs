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
        private const int CurrentSeedVersion = 2; // v2: redistributed POIs 45 m apart along Vinh Khanh street

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

            // POIs are distributed along Vinh Khanh street at ~45 m spacing (bearing 160° SSE).
            // dLat = -0.000381° per step, dLon = +0.000141° per step.
            // Radius = 30 m (display circle); narration trigger uses GeofenceService.TriggerRadiusMeters.

            var samplePOIs = new List<POI>
            {
                // POI #1 - Ốc Phát (Highest Priority) — position 1 (north end)
                new POI
                {
                    Name = "Ốc Phát",
                    Latitude = 10.761780, 
                    Longitude = 106.701800,
                    Radius = 30,
                    ApproachRadius = 200,
                    Priority = 10,
                    Description = "Quán ốc nổi tiếng trên đường Vĩnh Khánh",
                    TtsText = "Chào mừng bạn đến với Ốc Phát, quán ốc nổi tiếng trên đường Vĩnh Khánh.",
                    TtsTextEn = "Welcome to Oc Phat, a famous snail restaurant on Vinh Khanh Street.",
                    TtsTextKo = "빈칸 거리의 유명한 달팽이 레스토랑 옥팟에 오신 것을 환영합니다.",
                    TtsTextZh = "欢迎来到永庆街著名的蜗牛餐厅Ốc Phát。",
                    TtsTextJa = "ビンカン通りの有名なカタツムリレストラン、オック・ファットへようこそ。",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #2 - Quán Nước SINZIEN — position 2
                new POI
                {
                    Name = "Quán Nước SINZIEN",
                    Latitude = 10.763419,
                    Longitude = 106.701941,
                    Radius = 30,
                    ApproachRadius = 200,
                    Priority = 9,
                    Description = "Quán nước giải khát phong cách trẻ",
                    TtsText = "Bạn đang đến gần quán nước SINZIEN, nơi có nhiều loại đồ uống phong cách trẻ trung.",
                    TtsTextEn = "You are approaching SINZIEN cafe, a trendy beverage shop with youthful style.",
                    TtsTextKo = "청춘 스타일의 트렌디한 음료 가게인 SINZIEN 카페에 접근하고 있습니다.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #3 - Quán Ốc Thảo Quận 4 — position 3
                new POI
                {
                    Name = "Quán Ốc Thảo Quận 4",
                    Latitude = 10.763038,
                    Longitude = 106.702082,
                    Radius = 30,
                    ApproachRadius = 200,
                    Priority = 8,
                    Description = "Ốc tươi ngon với nhiều món chế biến",
                    TtsText = "Quán Ốc Thảo Quận 4, nơi phục vụ ốc tươi ngon với nhiều món chế biến đa dạng.",
                    TtsTextEn = "Oc Thao District 4, serving fresh snails with diverse cooking styles.",
                    TtsTextKo = "옥타오 4구, 다양한 조리 방법으로 신선한 달팽이를 제공합니다.",
                    TtsTextZh = "第4区蜗牛草，供应多种烹饪方式的新鲜蜗牛。",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #4 - Nướng Ngói Ti Ti — position 4
                new POI
                {
                    Name = "Nướng Ngói Ti Ti",
                    Latitude = 10.762657,
                    Longitude = 106.702223,
                    Radius = 30,
                    ApproachRadius = 200,
                    Priority = 7,
                    Description = "Đồ nướng trên ngói thơm ngon đặc biệt",
                    TtsText = "Nướng Ngói Ti Ti, chuyên các món nướng trên ngói thơm ngon đặc biệt.",
                    TtsTextEn = "Nuong Ngoi Ti Ti, specializing in delicious grilled dishes cooked on roof tiles.",
                    TtsTextKo = "누온응이티티, 기와 위에서 조리한 맛있는 구이 요리 전문점입니다.",
                    TtsTextZh = "瓦烤Ti Ti，专业在瓦上烤制美味菜肴。",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #5 - Đầu Trọc Tiệm Nướng — position 5
                new POI
                {
                    Name = "Đầu Trọc Tiệm Nướng",
                    Latitude = 10.762276,
                    Longitude = 106.702364,
                    Radius = 30,
                    ApproachRadius = 200,
                    Priority = 6,
                    Description = "Tiệm nướng với chủ đầu trọc nổi tiếng",
                    TtsText = "Đầu Trọc Tiệm Nướng, một tiệm nướng nổi tiếng với nhiều món ngon.",
                    TtsTextEn = "Dau Troc Barbeque, a well-known grill restaurant on Vinh Khanh Street.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #6 - Hải Ký Mì Gia — position 6 (centre of stretch)
                new POI
                {
                    Name = "Hải Ký Mì Gia",
                    Latitude = 10.761895,
                    Longitude = 106.702505,
                    Radius = 30,
                    ApproachRadius = 200,
                    Priority = 5,
                    Description = "Mì gia truyền hương vị đặc trưng",
                    TtsText = "Hải Ký Mì Gia, nơi phục vụ mì gia truyền với hương vị đặc trưng.",
                    TtsTextEn = "Hai Ky Noodle House, serving traditional noodles with a distinctive flavour.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #7 - Cơm Gà Như Thủy — position 7
                new POI
                {
                    Name = "Cơm Gà Như Thủy",
                    Latitude = 10.761514,
                    Longitude = 106.702646,
                    Radius = 30,
                    Priority = 4,
                    Description = "Cơm gà ngon nổi tiếng",
                    TtsText = "Cơm Gà Như Thủy, quán cơm gà ngon nổi tiếng trên đường Vĩnh Khánh.",
                    TtsTextEn = "Com Ga Nhu Thuy, famous for delicious chicken rice on Vinh Khanh Street.",
                    TtsTextKo = "콤가뉴튀이, 빈칸 거리의 유명한 닭고기 밥 레스토랑입니다.",
                    TtsTextZh = "如水鸡饭，永庆街著名的鸡肉饭店。",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #8 - Ốc 35k — position 8
                new POI
                {
                    Name = "Ốc 35k",
                    Latitude = 10.761133,
                    Longitude = 106.702787,
                    Radius = 30,
                    Priority = 3,
                    Description = "Ốc giá rẻ phù hợp sinh viên",
                    TtsText = "Ốc 35k, quán ốc giá rẻ phù hợp với sinh viên và người có thu nhập thấp.",
                    TtsTextEn = "Oc 35k, an affordable snail restaurant popular with students.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #9 - Khu Hải Sản Vĩnh Khánh — position 9
                new POI
                {
                    Name = "Khu Hải Sản Vĩnh Khánh",
                    Latitude = 10.760752,
                    Longitude = 106.702928,
                    Radius = 30,
                    Priority = 3,
                    Description = "Khu phố hải sản Vinh Khanh",
                    TtsText = "Chào mừng bạn đến với khu phố hải sản Vinh Khanh, nơi có nhiều quán ăn hải sản tươi ngon.",
                    TtsTextEn = "Welcome to the Vinh Khanh seafood zone, packed with fresh seafood restaurants.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #10 - Toàn Phương Quán — position 10
                new POI
                {
                    Name = "Toàn Phương Quán",
                    Latitude = 10.760371,
                    Longitude = 106.703069,
                    Radius = 30,
                    Priority = 3,
                    Description = "Quán ăn gia đình ấm cúng",
                    TtsText = "Toàn Phương Quán, một quán ăn gia đình ấm cúng với nhiều món ngon.",
                    TtsTextEn = "Toan Phuong Restaurant, a cosy family dining spot with a variety of dishes.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #11 - Ốc Vũ — position 11
                new POI
                {
                    Name = "Ốc Vũ",
                    Latitude = 10.759990,
                    Longitude = 106.703210,
                    Radius = 30,
                    Priority = 2,
                    Description = "Quán ốc đông khách trên đường Vĩnh Khánh",
                    TtsText = "Ốc Vũ, quán ốc đông khách với nhiều món ốc chế biến đa dạng.",
                    TtsTextEn = "Oc Vu, a busy snail restaurant with many different preparations.",
                    UseTts = true,
                    CooldownSeconds = 60,
                    IsActive = true
                },

                // POI #12 - Ốc Loan — position 12 (south end)
                new POI
                {
                    Name = "Ốc Loan",
                    Latitude = 10.759609,
                    Longitude = 106.703351,
                    Radius = 30,
                    Priority = 2,
                    Description = "Ốc tươi ngon giá cả phải chăng",
                    TtsText = "Ốc Loan, quán ốc tươi ngon với giá cả phải chăng.",
                    TtsTextEn = "Oc Loan, fresh snails at very affordable prices.",
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
