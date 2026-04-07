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

        /// <summary>
        /// Approaching radius - trigger when entering this zone (larger than main radius)
        /// </summary>
        public double ApproachRadius { get; set; } = 200;

        [NotNull]
        public int Priority { get; set; } = 1;

        public double Rating { get; set; } = 4.5;

        public int ReviewCount { get; set; } = 99;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(200)]
        public string AudioFile { get; set; } = string.Empty;

        [MaxLength(500)]
        public string TtsText { get; set; } = string.Empty;

        // Multi-language support
        [MaxLength(500)]
        public string TtsTextEn { get; set; } = string.Empty;

        [MaxLength(500)]
        public string TtsTextKo { get; set; } = string.Empty;

        [MaxLength(500)]
        public string TtsTextZh { get; set; } = string.Empty;

        [MaxLength(500)]
        public string TtsTextJa { get; set; } = string.Empty;

        public bool UseTts { get; set; } = false;

        [Ignore]
        public bool HasPlayed { get; set; } = false;

        [Ignore]
        public DateTime? LastTriggered { get; set; }

        [Ignore]
        public double LastDistance { get; set; } = double.MaxValue;

        public int CooldownSeconds { get; set; } = 300;

        public bool IsActive { get; set; } = true;

        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Get TTS text for specified language
        /// </summary>
        public string GetTtsText(string languageCode)
        {
            string text = languageCode.ToLower() switch
            {
                "vi" => TtsText,
                "en" => TtsTextEn,
                "ko" => TtsTextKo,
                "zh" => TtsTextZh,
                "ja" => TtsTextJa,
                _ => TtsText
            };

            // If no translation for this language, create generic template
            if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(Name))
            {
                text = languageCode.ToLower() switch
                {
                    "en" => $"You are approaching {Name}. This is a popular restaurant on Vinh Khanh Street.",
                    "ko" => $"{Name}에 접근하고 있습니다. 빈칸 거리의 인기 레스토랑입니다.",
                    "zh" => $"您正在接近{Name}。这是永庆街的热门餐厅。",
                    "ja" => $"{Name}に近づいています。ビンカン通りの人気レストランです。",
                    _ => TtsText
                };
            }

            return text ?? TtsText ?? string.Empty;
        }
    }
}
