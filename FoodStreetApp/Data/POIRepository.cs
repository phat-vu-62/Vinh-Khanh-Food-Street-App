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

        public async Task InitializeAsync()
        {
            var db = await GetDatabaseAsync();
            System.Diagnostics.Debug.WriteLine($"Database initialized at: {_dbPath}");

            var count = await db.Table<POI>().CountAsync();
            if (count == 0)
            {
                await SeedDataAsync();
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

            var samplePOIs = new List<POI>
            {
                new POI
                {
                    Name = "Ốc Phát",
                    Latitude = 10.7618166,
                    Longitude = 106.702175,
                    Radius = 50,
                    Priority = 2,
                    Description = "Quán ốc nổi tiếng trên đường Vĩnh Khánh",
                    AudioFile = "audio_ocphat.mp3",
                    TtsText = "Chào mừng bạn đến với Ốc Phát, quán ốc nổi tiếng trên đường Vĩnh Khánh.",
                    UseTts = false,
                    CooldownSeconds = 300,
                    IsActive = true
                },
                new POI
                {
                    Name = "Quán Nước SINZIEN",
                    Latitude = 10.7618166,
                    Longitude = 106.702175,
                    Radius = 50,
                    Priority = 2,
                    Description = "Quán nước giải khát phong cách trẻ",
                    AudioFile = "audio_nuocsinzien.mp3",
                    TtsText = "Bạn đang đến gần quán nước SINZIEN, nơi có nhiều loại đồ uống phong cách trẻ trung.",
                    UseTts = false,
                    CooldownSeconds = 300,
                    IsActive = true
                },
                new POI
                {
                    Name = "Quán Ốc Thảo Quận 4",
                    Latitude = 10.7618166,
                    Longitude = 106.702175,
                    Radius = 50,
                    Priority = 2,
                    Description = "Ốc tươi ngon với nhiều món chế biến",
                    AudioFile = "audio_octhao.mp3",
                    TtsText = "Quán Ốc Thảo Quận 4, nơi phục vụ ốc tươi ngon với nhiều món chế biến đa dạng.",
                    UseTts = false,
                    CooldownSeconds = 300,
                    IsActive = true
                },
                new POI
                {
                    Name = "Nướng Ngói Ti Ti",
                    Latitude = 10.7618419,
                    Longitude = 106.7020766,
                    Radius = 50,
                    Priority = 2,
                    Description = "Đồ nướng trên ngói thơm ngon đặc biệt",
                    AudioFile = "audio_nuongngoititi.mp3",
                    TtsText = "Nướng Ngói Ti Ti, chuyên các món nướng trên ngói thơm ngon đặc biệt.",
                    UseTts = false,
                    CooldownSeconds = 300,
                    IsActive = true
                },
                new POI
                {
                    Name = "Đầu Trọc Tiệm Nướng",
                    Latitude = 10.7616415,
                    Longitude = 106.7022233,
                    Radius = 50,
                    Priority = 2,
                    Description = "Tiệm nướng với chủ đầu trọc nổi tiếng",
                    AudioFile = "audio_dautroctiemnuong.mp3",
                    TtsText = "Đầu Trọc Tiệm Nướng, một tiệm nướng nổi tiếng với nhiều món ngon.",
                    UseTts = false,
                    CooldownSeconds = 300,
                    IsActive = true
                },
                new POI
                {
                    Name = "Hải Ký Mì Gia",
                    Latitude = 10.7614992,
                    Longitude = 106.7023145,
                    Radius = 50,
                    Priority = 2,
                    Description = "Mì gia truyền hương vị đặc trưng",
                    AudioFile = "audio_haikymigia.mp3",
                    TtsText = "Hải Ký Mì Gia, nơi phục vụ mì gia truyền với hương vị đặc trưng.",
                    UseTts = false,
                    CooldownSeconds = 300,
                    IsActive = true
                },
                new POI
                {
                    Name = "Cơm Gà Như Thủy",
                    Latitude = 10.7614788,
                    Longitude = 106.7023059,
                    Radius = 50,
                    Priority = 2,
                    Description = "Cơm gà ngon nổi tiếng",
                    AudioFile = "audio_comganhuthuy.mp3",
                    TtsText = "Cơm Gà Như Thủy, quán cơm gà ngon nổi tiếng trên đường Vĩnh Khánh.",
                    UseTts = false,
                    CooldownSeconds = 300,
                    IsActive = true
                },
                new POI
                {
                    Name = "Ốc 35k",
                    Latitude = 10.7614788,
                    Longitude = 106.7023059,
                    Radius = 50,
                    Priority = 2,
                    Description = "Ốc giá rẻ phù hợp sinh viên",
                    AudioFile = "audio_oc35k.mp3",
                    TtsText = "Ốc 35k, quán ốc giá rẻ phù hợp với sinh viên và người có thu nhập thấp.",
                    UseTts = false,
                    CooldownSeconds = 300,
                    IsActive = true
                },
                new POI
                {
                    Name = "빈칸 해산물 거리",
                    Latitude = 10.7614179,
                    Longitude = 106.7024029,
                    Radius = 50,
                    Priority = 2,
                    Description = "Khu phố hải sản Vinh Khanh",
                    AudioFile = "audio_binkanhaisanmulgeori.mp3",
                    TtsText = "Chào mừng bạn đến với khu phố hải sản Vinh Khanh, nơi có nhiều quán ăn hải sản tươi ngon.",
                    UseTts = false,
                    CooldownSeconds = 300,
                    IsActive = true
                },
                new POI
                {
                    Name = "Toàn Phương Quán",
                    Latitude = 10.7614992,
                    Longitude = 106.7023145,
                    Radius = 50,
                    Priority = 2,
                    Description = "Quán ăn gia đình ấm cúng",
                    AudioFile = "audio_toanphuongquan.mp3",
                    TtsText = "Toàn Phương Quán, một quán ăn gia đình ấm cúng với nhiều món ngon.",
                    UseTts = false,
                    CooldownSeconds = 300,
                    IsActive = true
                },
                new POI
                {
                    Name = "Ốc Vũ",
                    Latitude = 10.7614179,
                    Longitude = 106.7024029,
                    Radius = 50,
                    Priority = 2,
                    Description = "Quán ốc đông khách trên đường Vĩnh Khánh",
                    AudioFile = "audio_ocvu.mp3",
                    TtsText = "Ốc Vũ, quán ốc đông khách với nhiều món ốc chế biến đa dạng.",
                    UseTts = false,
                    CooldownSeconds = 300,
                    IsActive = true
                },
                new POI
                {
                    Name = "Ốc Loan",
                    Latitude = 10.7615414,
                    Longitude = 106.7023861,
                    Radius = 50,
                    Priority = 2,
                    Description = "Ốc tươi ngon giá cả phải chăng",
                    AudioFile = "audio_ocloan.mp3",
                    TtsText = "Ốc Loan, quán ốc tươi ngon với giá cả phải chăng.",
                    UseTts = false,
                    CooldownSeconds = 300,
                    IsActive = true
                }
            };

            foreach (var poi in samplePOIs)
            {
                await db.InsertAsync(poi);
            }

            System.Diagnostics.Debug.WriteLine($"Seeded {samplePOIs.Count} POIs to database");
        }
    }
}
