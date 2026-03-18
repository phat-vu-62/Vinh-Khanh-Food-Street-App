using System.ComponentModel;
using System.Globalization;
using System.Resources;
using FoodStreetApp.Resources;

namespace FoodStreetApp.Services
{
    public class LocalizationResourceManager : INotifyPropertyChanged
    {
        public static LocalizationResourceManager Instance { get; } = new();

        private LocalizationResourceManager()
        {
            var savedLang = Preferences.Get("app_ui_language", "en");
            SetCulture(new CultureInfo(savedLang));
        }

        public void SetCulture(CultureInfo culture)
        {
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // In MAUI, resx automatically follows CurrentUICulture, we just notify UI to refresh
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        public string this[string text]
        {
            get
            {
                // Giả lập Dịch ngôn ngữ, về sau bạn sẽ thay bằng ResourceManager thực sự
                // return AppStrings.ResourceManager.GetString(text, CultureInfo.CurrentUICulture) ?? text;
                return Translate(text);
            }
        }

        // Tạm dịch thủ công để bạn test (Do chưa có file .resx)
        private string Translate(string key)
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            if (culture == "vi")
            {
                return key switch
                {
                    "Home" => "Trang chủ",
                    "Map" => "Bản đồ",
                    "Settings" => "Cài đặt",
                    "Discover Vinh Khanh Food Street" => "Khám phá phố ẩm thực Vĩnh Khánh",
                    "Famous seafood street in District 4" => "Phố hải sản nổi tiếng tại Quận 4",
                    "Search restaurants..." => "Tìm kiếm nhà hàng...",
                    "🔥 Featured on Vinh Khanh" => "🔥 Nổi bật tại Vĩnh Khánh",
                    "⭐ Popular Seafood" => "⭐ Hải sản phổ biến",
                    "Vinh Khanh Street" => "Đường Vĩnh Khánh",
                    "🍽 All Restaurants" => "🍽 Tất cả nhà hàng",
                    "No restaurants found" => "Không tìm thấy nhà hàng",
                    "GPS & Location" => "GPS & Vị trí",
                    "Update frequency (seconds)" => "Tần suất cập nhật (giây)",
                    "Geofence" => "Khoanh vùng địa lý",
                    "Default trigger radius (meters)" => "Bán kính kích hoạt mặc định (mét)",
                    "Cooldown period (minutes)" => "Thời gian chờ (phút)",
                    "App Language" => "Ngôn ngữ ứng dụng",
                    "App Display Language" => "Ngôn ngữ hiển thị ứng dụng",
                    "Audio & TTS" => "Âm thanh & Đọc văn bản",
                    "Narration Language" => "Ngôn ngữ thuyết minh",
                    "Prefer TTS over audio files" => "Ưu tiên TTS thay vì file âm thanh",
                    "Enable audio notifications" => "Bật thông báo âm thanh",
                    "System Tests" => "Kiểm tra hệ thống",
                    "📍 Test GPS Location" => "📍 Thử nghiệm GPS",
                    "🔊 Test TTS" => "🔊 Thử nghiệm TTS",
                    "🎵 Test Narration Service" => "🎵 Thử nghiệm thuyết minh",
                    "Press GPS test to check your current location" => "Nhấn thử nghiệm GPS để kiểm tra vị trí hiện tại",
                    "Background Tracking" => "Theo dõi trong nền",
                    "Track even when app is minimized" => "Theo dõi ngay cả khi thu nhỏ ứng dụng",
                    "ℹ️ About" => "ℹ️ Thông tin",
                    "Vinh Khanh Food Street Guide" => "Hướng dẫn Phố ẩm thực Vĩnh Khánh",
                    "Built with .NET MAUI" => "Xây dựng bằng .NET MAUI",
                    "Reset" => "Đặt lại",
                    "All POI cooldowns have been reset" => "Đã đặt lại tất cả thời gian chờ của địa điểm",
                    "OK" => "Đồng ý",
                    "AppVersion" => "Phiên bản 2.0.0",
                    "Audio Guide" => "Hướng dẫn âm thanh",
                    "Listen to the story of this place" => "Lắng nghe câu chuyện của địa điểm này",
                    "Description" => "Mô tả",
                    "Show on Map" => "Hiển thị trên Bản đồ",
                    "Play" => "Phát",
                    "Stop" => "Dừng",
                    "Replay" => "Phát lại",
                    "seconds" => "giây",
                    "meters" => "mét",
                    "minutes" => "phút",
                    "Inside {0} zone" => "Trong khu vực {0}",
                    "Nearest POI: {0} — {1}m" => "Điểm gần nhất: {0} — {1}m",
                    "No POIs nearby" => "Không có điểm nào gần đây",
                    "Exploring Vinh Khanh Street" => "Đang khám phá đường Vĩnh Khánh",
                    "Quán ốc bình dân, đa dạng các loại ốc tươi ngon, nêm nếm đậm đà." => "Quán ốc bình dân, đa dạng các loại ốc tươi ngon, nêm nếm đậm đà.",
                    "Nổi tiếng với các món ốc xào me, nướng mỡ hành thơm lừng." => "Nổi tiếng với các món ốc xào me, nướng mỡ hành thơm lừng.",
                    "Kết hợp giữa đồ ăn ngon và bia tươi, cực kỳ sôi động về đêm." => "Kết hợp giữa đồ ăn ngon và bia tươi, cực kỳ sôi động về đêm.",
                    "Đồng giá 20k, phù hợp học sinh sinh viên, ngon và rẻ." => "Đồng giá 20k, phù hợp học sinh sinh viên, ngon và rẻ.",
                    "Ốc tươi sống, phục vụ nhanh, không gian thoáng mát." => "Ốc tươi sống, phục vụ nhanh, không gian thoáng mát.",
                    "Hải sản tươi sống, không gian gia đình ấm cúng." => "Hải sản tươi sống, không gian gia đình ấm cúng.",
                    "Quán ốc tươi ngon đầu đường Vĩnh Khánh" => "Quán ốc tươi ngon đầu đường Vĩnh Khánh",
                    "Quán ốc tươi ngon trên đường Vĩnh Khánh" => "Quán ốc tươi ngon trên đường Vĩnh Khánh",
                    "Quán ốc đông khách với nhiều món chế biến đa dạng" => "Quán ốc đông khách với nhiều món chế biến đa dạng",
                    "Quán ốc nổi tiếng trên đường Vĩnh Khánh" => "Quán ốc nổi tiếng trên đường Vĩnh Khánh",
                    "Nhà hàng lẩu đặc sắc trên đường Vĩnh Khánh" => "Nhà hàng lẩu đặc sắc trên đường Vĩnh Khánh",
                    "Quán ốc vỉa hè dân dã với nhiều món ngon" => "Quán ốc vỉa hè dân dã với nhiều món ngon",
                    "Điểm dừng chân giải khát trà sữa và cà phê" => "Điểm dừng chân giải khát trà sữa và cà phê",
                    "Nhà hàng bia và đồ ăn trên đường Vĩnh Khánh" => "Nhà hàng bia và đồ ăn trên đường Vĩnh Khánh",
                    "Quán ốc quen thuộc với nhiều món đặc trưng" => "Quán ốc quen thuộc với nhiều món đặc trưng",
                    "Quán ăn đặc biệt trên đường Vĩnh Khánh" => "Quán ăn đặc biệt trên đường Vĩnh Khánh",
                    "Quán ốc giá rẻ với nhiều món ngon" => "Quán ốc giá rẻ với nhiều món ngon",
                    "Quán ốc tươi với không gian thoải mái" => "Quán ốc tươi với không gian thoải mái",
                    "Nhà hàng lẩu gà lá é đặc biệt" => "Nhà hàng lẩu gà lá é đặc biệt",
                    "Quán ốc cuối đường Vĩnh Khánh" => "Quán ốc cuối đường Vĩnh Khánh",
                    _ => key
                };
            }
            if (culture == "en")
            {
                return key switch
                {
                    "All POI cooldowns have been reset" => "All POI cooldowns have been reset",
                    "OK" => "OK",
                    "AppVersion" => "Version 2.0.0",
                    "Quán ốc bình dân, đa dạng các loại ốc tươi ngon, nêm nếm đậm đà." => "Affordable snail restaurant, diverse fresh snails, rich flavors.",
                    "Nổi tiếng với các món ốc xào me, nướng mỡ hành thơm lừng." => "Famous for tamarind stir-fried snails, fragrant scallion fat grilling.",
                    "Kết hợp giữa đồ ăn ngon và bia tươi, cực kỳ sôi động về đêm." => "Combining delicious food and draft beer, extremely lively at night.",
                    "Đồng giá 20k, phù hợp học sinh sinh viên, ngon và rẻ." => "Flat price 20k, suitable for students, tasty and cheap.",
                    "Ốc tươi sống, phục vụ nhanh, không gian thoáng mát." => "Fresh live snails, fast service, airy space.",
                    "Hải sản tươi sống, không gian gia đình ấm cúng." => "Fresh live seafood, cozy family space.",
                    "seconds" => "seconds",
                    "meters" => "meters",
                    "minutes" => "minutes",
                    "Inside {0} zone" => "Inside {0} zone",
                    "Nearest POI: {0} — {1}m" => "Nearest POI: {0} — {1}m",
                    "No POIs nearby" => "No POIs nearby",
                    "Exploring Vinh Khanh Street" => "Exploring Vinh Khanh Street",
                    "Quán ốc tươi ngon đầu đường Vĩnh Khánh" => "Fresh and tasty snail restaurant at the beginning of Vinh Khanh street",
                    "Quán ốc tươi ngon trên đường Vĩnh Khánh" => "Fresh and tasty snail restaurant on Vinh Khanh street",
                    "Quán ốc đông khách với nhiều món chế biến đa dạng" => "Crowded snail restaurant with diverse cooked dishes",
                    "Quán ốc nổi tiếng trên đường Vĩnh Khánh" => "Famous snail restaurant on Vinh Khanh street",
                    "Nhà hàng lẩu đặc sắc trên đường Vĩnh Khánh" => "Outstanding hot pot restaurant on Vinh Khanh street",
                    "Quán ốc vỉa hè dân dã với nhiều món ngon" => "Rustic sidewalk snail eatery with many delicious dishes",
                    "Điểm dừng chân giải khát trà sữa và cà phê" => "Rest stop for refreshing milk tea and coffee",
                    "Nhà hàng bia và đồ ăn trên đường Vĩnh Khánh" => "Beer and food restaurant on Vinh Khanh street",
                    "Quán ốc quen thuộc với nhiều món đặc trưng" => "Familiar snail restaurant with many signature dishes",
                    "Quán ăn đặc biệt trên đường Vĩnh Khánh" => "Special eatery on Vinh Khanh street",
                    "Quán ốc giá rẻ với nhiều món ngon" => "Cheap snail restaurant with many delicious dishes",
                    "Quán ốc tươi với không gian thoải mái" => "Fresh snails with a comfortable atmosphere",
                    "Nhà hàng lẩu gà lá é đặc biệt" => "Special chicken and é leaf hotpot restaurant",
                    "Quán ốc cuối đường Vĩnh Khánh" => "Snail restaurant at the end of Vinh Khanh street",
                    _ => key
                };
            }
            if (culture == "ko")
            {
                return key switch
                {
                    "Home" => "홈",
                    "Map" => "지도",
                    "Settings" => "설정",
                    "Discover Vinh Khanh Food Street" => "빈칸 음식 거리 발견",
                    "Famous seafood street in District 4" => "4군의 유명한 해산물 거리",
                    "Search restaurants..." => "레스토랑 검색...",
                    "🔥 Featured on Vinh Khanh" => "🔥 빈칸 추천 요리",
                    "⭐ Popular Seafood" => "⭐ 인기 해산물",
                    "Vinh Khanh Street" => "빈칸 거리",
                    "🍽 All Restaurants" => "🍽 모든 레스토랑",
                    "No restaurants found" => "식당을 찾을 수 없습니다",
                    "GPS & Location" => "GPS 및 위치",
                    "Update frequency (seconds)" => "업데이트 빈도(초)",
                    "Geofence" => "지오펜스",
                    "Default trigger radius (meters)" => "기본 트리거 반경(미터)",
                    "Cooldown period (minutes)" => "재사용 대기시간(분)",
                    "App Language" => "앱 언어",
                    "App Display Language" => "앱 표시 언어",
                    "Audio & TTS" => "오디오 및 자동 읽기",
                    "Narration Language" => "내레이션 언어",
                    "Prefer TTS over audio files" => "오디오 파일보다 TTS 선호",
                    "Enable audio notifications" => "오디오 알림 활성화",
                    "System Tests" => "시스템 테스트",
                    "📍 Test GPS Location" => "📍 GPS 위치 테스트",
                    "🔊 Test TTS" => "🔊 TTS 테스트",
                    "🎵 Test Narration Service" => "🎵 내레이션 서비스 테스트",
                    "Press GPS test to check your current location" => "GPS 테스트를 눌러 현재 위치를 확인하십시오",
                    "Background Tracking" => "백그라운드 추적",
                    "Track even when app is minimized" => "앱이 최소화된 경우에도 위치 추적",
                    "ℹ️ About" => "ℹ️ 정보",
                    "Vinh Khanh Food Street Guide" => "빈칸 음식 거리 안내",
                    "Built with .NET MAUI" => ".NET MAUI로 구축됨",
                    "Reset" => "초기화",
                    "All POI cooldowns have been reset" => "모든 POI 재사용 대기시간이 초기화되었습니다",
                    "OK" => "확인",
                    "AppVersion" => "버전 2.0.0",
                    "Audio Guide" => "오디오 가이드",
                    "Listen to the story of this place" => "이 장소의 이야기를 들어보세요",
                    "Description" => "설명",
                    "Show on Map" => "지도에 표시",
                    "Play" => "재생",
                    "Stop" => "정지",
                    "Replay" => "다시 재생",
                    "seconds" => "초",
                    "meters" => "미터",
                    "minutes" => "분",
                    "Inside {0} zone" => "{0} 구역 내부",
                    "Nearest POI: {0} — {1}m" => "가장 가까운 POI: {0} — {1}m",
                    "No POIs nearby" => "근처에 POI 없음",
                    "Exploring Vinh Khanh Street" => "빈칸 거리 탐험",
                    "Quán ốc bình dân, đa dạng các loại ốc tươi ngon, nêm nếm đậm đà." => "풍부한 맛의 다양한 신선한 달팽이가 있는 저렴한 달팽이 식당.",
                    "Nổi tiếng với các món ốc xào me, nướng mỡ hành thơm lừng." => "타마린드 볶음 달팽이, 향긋한 파기름 구이로 유명함.",
                    "Kết hợp giữa đồ ăn ngon와 bia tươi, cực kỳ sôi động về đêm." => "맛있는 음식과 생맥주의 결합, 밤에 매우 활기차다.",
                    "Kết hợp giữa đồ ăn ngon và bia tươi, cực kỳ sôi động về đêm." => "맛있는 음식과 생맥주의 결합, 밤에 매우 활기차다.",
                    "Đồng giá 20k, phù hợp học sinh sinh viên, ngon và rẻ." => "전품목 20k, 학생에게 적합, 맛있고 저렴함.",
                    "Ốc tươi sống, phục vụ nhanh, không gian thoáng mát." => "신선한 살아있는 달팽이, 빠른 서비스, 통풍이 잘되는 공간.",
                    "Hải sản tươi sống, không gian gia đình ấm cúng." => "신선한 살아있는 해산물, 아늑한 가족 공간.",
                    "Quán ốc tươi ngon đầu đường Vĩnh Khánh" => "빈칸 거리 시작 부분의 신선하고 맛있는 달팽이 식당",
                    "Quán ốc tươi ngon trên đường Vĩnh Khánh" => "빈칸 거리의 신선하고 맛있는 달팽이 식당",
                    "Quán ốc đông khách với nhiều món chế biến đa dạng" => "다양한 요리가 있는 붐비는 달팽이 식당",
                    "Quán ốc nổi tiếng trên đường Vĩnh Khánh" => "빈칸 거리의 유명한 달팽이 식당",
                    "Nhà hàng lẩu đặc sắc trên đường Vĩnh Khánh" => "빈칸 거리의 뛰어난 핫팟 레스토랑",
                    "Quán ốc vỉa hè dân dã với nhiều món ngon" => "맛있는 요리가 많은 소박한 길거리 달팽이 식당",
                    "Điểm dừng chân giải khát trà sữa và cà phê" => "상쾌한 밀크티와 커피를 위한 휴식처",
                    "Nhà hàng bia và đồ ăn trên đường Vĩnh Khánh" => "빈칸 거리의 맥주 및 음식 레스토랑",
                    "Quán ốc quen thuộc với nhiều món đặc trưng" => "많은 특선 요리가 있는 친숙한 달팽이 식당",
                    "Quán ăn đặc biệt trên đường Vĩnh Khánh" => "빈칸 거리의 특별한 식당",
                    "Quán ốc giá rẻ với nhiều món ngon" => "맛있는 요리가 많은 저렴한 달팽이 식당",
                    "Quán ốc tươi với không gian thoải mái" => "편안한 분위기의 신선한 달팽이",
                    "Nhà hàng lẩu gà lá é đặc biệt" => "특별한 닭고기 및 허브 핫팟 레스토랑",
                    "Quán ốc cuối đường Vĩnh Khánh" => "빈칸 거리 끝의 달팽이 식당",
                    _ => key
                };
            }
            if (culture == "ja")
            {
                return key switch
                {
                    "Home" => "ホーム",
                    "Map" => "マップ",
                    "Settings" => "設定",
                    "Discover Vinh Khanh Food Street" => "ヴィンカン通りを発見",
                    "Famous seafood street in District 4" => "4区の有名なシーフードストリート",
                    "Search restaurants..." => "レストランを検索...",
                    "🔥 Featured on Vinh Khanh" => "🔥 ヴィンカンのおすすめ",
                    "⭐ Popular Seafood" => "⭐ 人気のシーフード",
                    "Vinh Khanh Street" => "ヴィンカン通り",
                    "🍽 All Restaurants" => "🍽 すべてのレストラン",
                    "No restaurants found" => "レストランが見つかりません",
                    "GPS & Location" => "GPSと位置情報",
                    "Update frequency (seconds)" => "更新頻度（秒）",
                    "Geofence" => "ジオフェンス",
                    "Default trigger radius (meters)" => "デフォルトのトリガー半径（メートル）",
                    "Cooldown period (minutes)" => "クールダウン時間（分）",
                    "App Language" => "アプリの言語",
                    "App Display Language" => "アプリの表示言語",
                    "Audio & TTS" => "オーディオとTTS",
                    "Narration Language" => "ナレーション言語",
                    "Prefer TTS over audio files" => "オーディオファイルよりTTSを優先する",
                    "Enable audio notifications" => "音声通知を有効にする",
                    "System Tests" => "システムテスト",
                    "📍 Test GPS Location" => "📍 GPS位置情報をテスト",
                    "🔊 Test TTS" => "🔊 TTSをテスト",
                    "🎵 Test Narration Service" => "🎵 ナレーションサービスをテスト",
                    "Press GPS test to check your current location" => "GPSテストを押して現在の位置を確認します",
                    "Background Tracking" => "バックグラウンド追跡",
                    "Track even when app is minimized" => "アプリが最小化されている場合でも追跡する",
                    "ℹ️ About" => "ℹ️ 概要",
                    "Vinh Khanh Food Street Guide" => "ヴィンカンフードストリートガイド",
                    "Built with .NET MAUI" => ".NET MAUIで構築",
                    "Reset" => "リセット",
                    "All POI cooldowns have been reset" => "すべてのPOIクールダウンがリセットされました",
                    "OK" => "OK",
                    "AppVersion" => "バージョン 2.0.0",
                    "Audio Guide" => "オーディオガイド",
                    "Listen to the story of this place" => "この場所のストーリーを聞く",
                    "Description" => "説明",
                    "Show on Map" => "地図に表示",
                    "seconds" => "秒",
                    "meters" => "メートル",
                    "minutes" => "分",
                    "Inside {0} zone" => "{0} ゾーン内",
                    "Nearest POI: {0} — {1}m" => "最も近いPOI: {0} — {1}m",
                    "No POIs nearby" => "近くにPOIはありません",
                    "Exploring Vinh Khanh Street" => "ヴィンカン通りを探索",
                    "Quán ốc bình dân, đa dạng các loại ốc tươi ngon, nêm nếm đậm đà." => "豊富な味の多様な新鮮なカタツムリがある手頃なレストラン。",
                    "Nổi tiếng với các món ốc xào me, nướng mỡ hành thơm lừng." => "タマリンド炒めカタツムリ、香り高いネギ油焼きで有名。",
                    "Kết hợp giữa đồ ăn ngon và bia tươi, cực kỳ sôi động về đêm." => "美味しい料理と生ビールの組み合わせ、夜はとても活気がある。",
                    "Đồng giá 20k, phù hợp học sinh sinh viên, ngon và rẻ." => "全品20k、学生向け、美味しくて安い。",
                    "Ốc tươi sống, phục vụ nhanh, không gian thoáng mát." => "新鮮な活カタツムリ、迅速なサービス、風通しの良い空間。",
                    "Hải sản tươi sống, không gian gia đình ấm cúng." => "新鮮な活シーフード、居心地の良い家族の空間。",
                    "Quán ốc tươi ngon đầu đường Vĩnh Khánh" => "ヴィンカン通りの入り口にある新鮮で美味しいカタツムリレストラン",
                    "Quán ốc tươi ngon trên đường Vĩnh Khánh" => "ヴィンカン通りにある新鮮で美味しいカタツムリレストラン",
                    "Quán ốc đông khách với nhiều món chế biến đa dạng" => "多様な料理がある混雑したカタツムリレストラン",
                    "Quán ốc nổi tiếng trên đường Vĩnh Khánh" => "ヴィンカン通りにある有名なカタツムリレストラン",
                    "Nhà hàng lẩu đặc sắc trên đường Vĩnh Khánh" => "ヴィンカン通りにある優れた火鍋レストラン",
                    "Quán ốc vỉa hè dân dã với nhiều món ngon" => "美味しい料理がたくさんある素朴な屋台のカタツムリ店",
                    "Điểm dừng chân giải khát trà sữa và cà phê" => "爽やかなミルクティーとコーヒーの休憩所",
                    "Nhà hàng bia và đồ ăn trên đường Vĩnh Khánh" => "ヴィンカン通りにあるビールと食事のレストラン",
                    "Quán ốc quen thuộc với nhiều món đặc trưng" => "多くの特製料理があるおなじみのカタツムリレストラン",
                    "Quán ăn đặc biệt trên đường Vĩnh Khánh" => "ヴィンカン通りにある特別な食堂",
                    "Quán ốc giá rẻ với nhiều món ngon" => "美味しい料理がたくさんある安いカタツムリレストラン",
                    "Quán ốc tươi với không gian thoải mái" => "快適な雰囲気の新鮮なカタツムリ",
                    "Nhà hàng lẩu gà lá é đặc biệt" => "特別な鶏肉とハーブの火鍋レストラン",
                    "Quán ốc cuối đường Vĩnh Khánh" => "ヴィンカン通りの終わりにあるカタツムリレストラン",
                    _ => key
                };
            }
            if (culture == "zh")
            {
                return key switch
                {
                    "Home" => "首页",
                    "Map" => "地图",
                    "Settings" => "设置",
                    "Discover Vinh Khanh Food Street" => "探索 Vinh Khanh 美食街",
                    "Famous seafood street in District 4" => "第四郡著名的海鲜街",
                    "Search restaurants..." => "搜索餐厅...",
                    "🔥 Featured on Vinh Khanh" => "🔥 Vinh Khanh精选",
                    "⭐ Popular Seafood" => "⭐ 热门海鲜",
                    "Vinh Khanh Street" => "Vinh Khanh区",
                    "🍽 All Restaurants" => "🍽 所有餐厅",
                    "No restaurants found" => "未找到餐厅",
                    "GPS & Location" => "GPS与位置",
                    "Update frequency (seconds)" => "更新频率（秒）",
                    "Geofence" => "地理围栏",
                    "Default trigger radius (meters)" => "默认触发半径（米）",
                    "Cooldown period (minutes)" => "冷却时间（分钟）",
                    "App Language" => "应用语言",
                    "App Display Language" => "应用显示语言",
                    "Audio & TTS" => "音频与TTS",
                    "Narration Language" => "解说语言",
                    "Prefer TTS over audio files" => "优先使用TTS而不是音频文件",
                    "Enable audio notifications" => "启用音频通知",
                    "System Tests" => "系统测试",
                    "📍 Test GPS Location" => "📍 测试GPS位置",
                    "🔊 Test TTS" => "🔊 测试TTS",
                    "🎵 Test Narration Service" => "🎵 测试解说服务",
                    "Press GPS test to check your current location" => "按GPS测试以检查您当前的位置",
                    "Background Tracking" => "后台跟踪",
                    "Track even when app is minimized" => "即使应用最小化也进行跟踪",
                    "ℹ️ About" => "ℹ️ 关于",
                    "Vinh Khanh Food Street Guide" => "Vinh Khanh 美食街指南",
                    "Built with .NET MAUI" => "使用 .NET MAUI 构建",
                    "Reset" => "重置",
                    "All POI cooldowns have been reset" => "所有POI冷却时间已重置",
                    "OK" => "确定",
                    "AppVersion" => "版本 2.0.0",
                    "Audio Guide" => "语音指南",
                    "Listen to the story of this place" => "听听这个地方的故事",
                    "Description" => "描述",
                    "Show on Map" => "在地图上显示",
                    "Play" => "播放",
                    "Stop" => "停止",
                    "Replay" => "重播",
                    "seconds" => "秒",
                    "meters" => "米",
                    "minutes" => "分钟",
                    "Inside {0} zone" => "在 {0} 区域内",
                    "Nearest POI: {0} — {1}m" => "最近的兴趣点: {0} — {1}m",
                    "No POIs nearby" => "附近没有兴趣点",
                    "Exploring Vinh Khanh Street" => "探索 Vinh Khanh 街",
                    "Quán ốc bình dân, đa dạng các loại ốc tươi ngon, nêm nếm đậm đà." => "平价蜗牛餐厅，各式新鲜蜗牛，味道浓郁。",
                    "Nổi tiếng với các món ốc xào me, nướng mỡ hành thơm lừng." => "以罗望子炒蜗牛、香葱油烤著名。",
                    "Kết hợp giữa đồ ăn ngon và bia tươi, cực kỳ sôi động về đêm." => "美味的食物与生啤的结合，晚上非常热闹。",
                    "Đồng giá 20k, phù hợp học sinh sinh viên, ngon và rẻ." => "统一价20k，适合学生，好吃又便宜。",
                    "Ốc tươi sống, phục vụ nhanh, không gian thoáng mát." => "新鲜活蜗牛，服务快，空间通风。",
                    "Hải sản tươi sống, không gian gia đình ấm cúng." => "新鲜活海鲜，温馨的家庭空间。",
                    "Quán ốc tươi ngon đầu đường Vĩnh Khánh" => "Vinh Khanh 街头新鲜美味的蜗牛餐厅",
                    "Quán ốc tươi ngon trên đường Vĩnh Khánh" => "Vinh Khanh 街上新鲜美味的蜗牛餐厅",
                    "Quán ốc đông khách với nhiều món chế biến đa dạng" => "菜品多样的熙熙攘攘的蜗牛餐厅",
                    "Quán ốc nổi tiếng trên đường Vĩnh Khánh" => "Vinh Khanh 街上著名的蜗牛餐厅",
                    "Nhà hàng lẩu đặc sắc trên đường Vĩnh Khánh" => "Vinh Khanh 街上出色的火锅店",
                    "Quán ốc vỉa hè dân dã với nhiều món ngon" => "有很多美食的质朴的街头蜗牛小摊",
                    "Điểm dừng chân giải khát trà sữa và cà phê" => "清爽奶茶和咖啡的休息站",
                    "Nhà hàng bia và đồ ăn trên đường Vĩnh Khánh" => "Vinh Khanh 街上的啤酒和美食餐厅",
                    "Quán ốc quen thuộc với nhiều món đặc trưng" => "有许多招牌菜的熟悉的蜗牛餐厅",
                    "Quán ăn đặc biệt trên đường Vĩnh Khánh" => "Vinh Khanh 街上的特色餐厅",
                    "Quán ốc giá rẻ với nhiều món ngon" => "有很多美食的便宜的蜗牛餐厅",
                    "Quán ốc tươi với không gian thoải mái" => "气氛舒适的新鲜蜗牛",
                    "Nhà hàng lẩu gà lá é đặc biệt" => "特别的鸡肉和香草火锅店",
                    "Quán ốc cuối đường Vĩnh Khánh" => "Vinh Khanh 街尾的蜗牛餐厅",
                    _ => key
                };
            }

            return key; // Default English
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
