# 🌍 Multi-Language & Approaching Detection - Feature Guide

## ✨ Tính Năng Mới

### 1️⃣ **Approaching Detection** (Phát Âm Thanh Khi Tiến Gần)
Thay vì phát âm thanh khi **đã ở trong** vùng POI, app bây giờ phát khi bạn **đang tiến gần** vào quán.

**Cách hoạt động:**
- Mỗi POI có 2 vùng bán kính:
  - `ApproachRadius` (200m) - Vùng tiếp cận
  - `Radius` (50m) - Vùng chính
- Khi bạn **di chuyển từ xa → gần** và vào trong `ApproachRadius` → Phát thuyết minh
- Logic tracking: So sánh `distance` hiện tại với `LastDistance`

**Ví dụ:**
```
User ở 250m → 220m → 180m (TIẾN GẦN + VÀO ApproachRadius) → 🔊 PHÁT ÂM THANH
User ở 150m → 170m → 190m (RỜI XA) → Không phát
```

---

### 2️⃣ **Multi-Language Support** (Hỗ Trợ Nhiều Ngôn Ngữ)
App bây giờ hỗ trợ **5 ngôn ngữ** cho thuyết minh:
- 🇻🇳 **Vietnamese** (Tiếng Việt)
- 🇬🇧 **English**
- 🇰🇷 **Korean** (한국어)
- 🇨🇳 **Chinese** (中文)
- 🇯🇵 **Japanese** (日本語)

**Tính năng:**
- Mỗi POI có thuyết minh cho tất cả ngôn ngữ
- User chọn ngôn ngữ trong **Settings**
- TTS tự động phát đúng ngôn ngữ với giọng đọc phù hợp
- Nếu POI không có text cho ngôn ngữ đã chọn → Fallback to Vietnamese

---

## 🛠️ Implementation Details

### POI Model Changes

```csharp
public class POI
{
    // Existing fields...
    public double Radius { get; set; }  // Main radius (50m)
    
    // NEW: Approaching radius
    public double ApproachRadius { get; set; } = 200;  // Trigger at 200m
    
    // NEW: Multi-language support
    public string TtsText { get; set; }      // Vietnamese (default)
    public string TtsTextEn { get; set; }    // English
    public string TtsTextKo { get; set; }    // Korean
    public string TtsTextZh { get; set; }    // Chinese
    public string TtsTextJa { get; set; }    // Japanese
    
    // NEW: Track distance for approaching detection
    [Ignore]
    public double LastDistance { get; set; } = double.MaxValue;
    
    // Helper method
    public string GetTtsText(string languageCode)
    {
        return languageCode.ToLower() switch
        {
            "vi" => TtsText,
            "en" => !string.IsNullOrEmpty(TtsTextEn) ? TtsTextEn : TtsText,
            "ko" => !string.IsNullOrEmpty(TtsTextKo) ? TtsTextKo : TtsText,
            "zh" => !string.IsNullOrEmpty(TtsTextZh) ? TtsTextZh : TtsText,
            "ja" => !string.IsNullOrEmpty(TtsTextJa) ? TtsTextJa : TtsText,
            _ => TtsText
        };
    }
}
```

### GeofenceService Changes

```csharp
public POI? CheckGeofences(Location currentLocation)
{
    foreach (var poi in _pois)
    {
        var distance = CalculateDistanceInMeters(...);
        
        // NEW: Check if approaching (getting closer)
        bool isApproaching = distance < poi.LastDistance;
        poi.LastDistance = distance;
        
        // NEW: Trigger when ENTERING approach radius AND moving towards POI
        if (distance < poi.ApproachRadius && 
            cooldownExpired && 
            isApproaching && 
            distance < minDistance)
        {
            triggeredPoi = poi;
        }
    }
    
    if (triggeredPoi != null)
    {
        await _narrationService.PlayNarrationAsync(triggeredPoi);
    }
}
```

### NarrationService Changes

```csharp
public class NarrationService : INarrationService
{
    private string _currentLanguage = "vi";
    
    public void SetLanguage(string languageCode)
    {
        _currentLanguage = languageCode;
        Preferences.Set("app_language", languageCode);
    }
    
    public async Task PlayNarrationAsync(POI poi)
    {
        // Get text in selected language
        var ttsText = poi.GetTtsText(_currentLanguage);
        
        // Get TTS locale for language
        var locale = GetLocaleForLanguage(_currentLanguage);
        
        // Speak with correct locale
        await TextToSpeech.Default.SpeakAsync(ttsText, new SpeechOptions
        {
            Locale = locale
        });
    }
    
    private Locale? GetLocaleForLanguage(string languageCode)
    {
        var locales = TextToSpeech.Default.GetLocalesAsync().Result;
        return locales?.FirstOrDefault(l => 
            l.Language.StartsWith(languageCode, StringComparison.OrdinalIgnoreCase));
    }
}
```

### Settings UI

```xaml
<!-- Language Picker -->
<Picker x:Name="LanguagePicker"
        Title="Chọn ngôn ngữ / Select Language"
        ItemsSource="{Binding AvailableLanguages}"
        ItemDisplayBinding="{Binding NativeName}"
        SelectedItem="{Binding SelectedLanguage, Mode=TwoWay}" />
```

---

## 📊 Database Seed Example

```csharp
new POI
{
    Name = "Ốc Phát",
    Latitude = 10.7618166,
    Longitude = 106.702175,
    Radius = 50,              // Main zone
    ApproachRadius = 200,     // Approaching zone
    
    // Multi-language texts
    TtsText = "Chào mừng bạn đến với Ốc Phát...",
    TtsTextEn = "Welcome to Oc Phat, a famous snail restaurant...",
    TtsTextKo = "빈칸 거리의 유명한 달팽이 레스토랑...",
    TtsTextZh = "欢迎来到永庆街著名的蜗牛餐厅...",
    TtsTextJa = "ビンカン通りの有名なカタツムリレストラン...",
    
    UseTts = true,
    CooldownSeconds = 30,
    IsActive = true
}
```

---

## 🧪 Testing

### Test 1: Approaching Detection

1. **Setup:**
   - Mock GPS hoặc thiết bị thật
   - Mở app → Map page

2. **Scenario:**
   ```
   Position 1: 300m từ POI (xa)
   → Di chuyển
   Position 2: 180m từ POI (đang tiến gần, trong ApproachRadius)
   → 🔊 Should trigger narration
   ```

3. **Expected Logs:**
   ```
   >>> LOCATION UPDATE: ...
   POI: Ốc Phát | Distance: 180.00m | ApproachRadius: 200m | Approaching: True
   >>> 🎯 APPROACHING POI: Ốc Phát at 180.00m
   >>> 🔊 Playing TTS (vi): Ốc Phát
   ```

### Test 2: Multi-Language

1. **Setup:**
   - Mở Settings
   - Chọn ngôn ngữ (English, Korean, etc.)

2. **Test:**
   - Nhấn "Test TTS" button
   - Hoặc trigger geofence

3. **Expected:**
   - Nghe thuyết minh bằng ngôn ngữ đã chọn
   - TTS dùng giọng đọc phù hợp

4. **Logs:**
   ```
   >>> Language: en
   >>> 🔊 Playing TTS (en): Ốc Phát
   >>> TTS Text: Welcome to Oc Phat, a famous snail restaurant...
   >>> Using TTS locale: en-US - United States
   ```

---

## 🎯 User Experience Flow

### Flow 1: Tourist Walking Tour

```
1. Tourist mở app, chọn ngôn ngữ English
2. Bắt đầu đi bộ trên đường Vĩnh Khánh
3. Khi tiến gần Ốc Phát (từ 250m → 180m):
   → App tự động phát: "Welcome to Oc Phat..."
4. Tourist tiếp tục đi
5. Sau 30 giây (cooldown), khi tiến gần quán khác:
   → Phát thuyết minh quán đó
```

### Flow 2: Language Switching

```
1. User đang dùng Vietnamese
2. Vào Settings → Chọn Korean
3. Test TTS → Nghe thử giọng Korean
4. Quay lại Map → Di chuyển
5. Khi trigger geofence → Nghe thuyết minh Korean
```

---

## 📱 Settings Options

| Setting | Default | Description |
|---------|---------|-------------|
| **Ngôn ngữ** | Tiếng Việt | Chọn ngôn ngữ thuyết minh |
| **ApproachRadius** | 200m | Khoảng cách trigger (có thể điều chỉnh trong Settings) |
| **Cooldown** | 30s | Thời gian chờ giữa các lần phát |

---

## 🌟 Benefits

### Approaching Detection
✅ Phát sớm hơn → User có thời gian nghe kỹ
✅ Không bị bất ngờ khi đã vào trong quán
✅ User có thể quyết định có vào hay không

### Multi-Language
✅ Hỗ trợ du khách quốc tế
✅ Mở rộng thị trường (Hàn Quốc, Trung Quốc, Nhật Bản...)
✅ Tự động dùng giọng TTS phù hợp
✅ Fallback to Vietnamese nếu thiếu translation

---

## 🔧 Adding New Languages

Để thêm ngôn ngữ mới:

### 1. Update POI Model
```csharp
[MaxLength(500)]
public string TtsTextFr { get; set; } = string.Empty;  // French
```

### 2. Update GetTtsText Method
```csharp
public string GetTtsText(string languageCode)
{
    return languageCode.ToLower() switch
    {
        // ...existing
        "fr" => !string.IsNullOrEmpty(TtsTextFr) ? TtsTextFr : TtsText,
        _ => TtsText
    };
}
```

### 3. Update SettingsViewModel
```csharp
AvailableLanguages = new ObservableCollection<LanguageOption>
{
    // ...existing
    new LanguageOption { Code = "fr", Name = "French", NativeName = "Français" }
};
```

### 4. Update Database Seed
```csharp
new POI
{
    // ...existing
    TtsTextFr = "Bienvenue à Oc Phat...",
}
```

---

## 🐛 Troubleshooting

### Issue: Không phát khi tiến gần
**Nguyên nhân:**
- `LastDistance` không được reset
- User đang di chuyển ra xa (not approaching)

**Giải pháp:**
- Reset geofence để clear `LastDistance`
- Đảm bảo đang di chuyển VÀO GẦN (distance giảm dần)

### Issue: TTS phát sai giọng
**Nguyên nhân:**
- Thiết bị không có TTS engine cho ngôn ngữ đó

**Giải pháp:**
- Download TTS engine từ Play Store/App Store
- App tự động fallback to Vietnamese nếu không có

---

**🎉 Enjoy the enhanced features!**
