using SQLite;

namespace FoodStreetApp.Models
{
    [Table("pois")]
    public class POI
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(200), NotNull]
        public string Name { get; set; } = string.Empty;

        [NotNull]
        public double Latitude { get; set; }

        [NotNull]
        public double Longitude { get; set; }

        [NotNull]
        public double Radius { get; set; }

        [NotNull]
        public int Priority { get; set; } = 1;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(200)]
        public string AudioFile { get; set; } = string.Empty;

        [MaxLength(500)]
        public string TtsText { get; set; } = string.Empty;

        public bool UseTts { get; set; } = false;

        [Ignore]
        public bool HasPlayed { get; set; } = false;

        [Ignore]
        public DateTime? LastTriggered { get; set; }

        public int CooldownSeconds { get; set; } = 300;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
