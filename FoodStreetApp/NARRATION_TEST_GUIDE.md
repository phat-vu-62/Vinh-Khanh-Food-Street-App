# 🎵 Hướng Dẫn Test Thuyết Minh (Narration)

## ✅ App đã có đầy đủ tính năng thuyết minh!

Hệ thống thuyết minh đã được implement với:
- **NarrationService**: Xử lý TTS và phát audio
- **GeofenceService**: Tự động trigger thuyết minh khi vào vùng POI
- **AudioService**: Phát file audio
- Hỗ trợ cả Text-to-Speech (TTS) và Audio Files

---

## 🧪 Cách Test

### 1. Test Trực Tiếp (Đã thêm mới)
1. Mở app và vào trang **Cài Đặt** (Settings)
2. Kéo xuống phần **"Audio & TTS"**
3. Nhấn nút **"🔊 Test TTS"** - Sẽ nghe thấy test bằng TTS
4. Nhấn nút **"🎵 Test Narration Service"** - Test service đầy đủ

### 2. Test Với GPS Thật
1. Bật quyền GPS trên thiết bị
2. Mở app và vào trang **Map**
3. Đi đến gần tọa độ POI:
   - Ốc Phát: `10.7618166, 106.702175`
   - Bán kính: **200m** (đã tăng từ 50m để test dễ hơn)
4. Khi vào vùng, sẽ tự động phát thuyết minh

### 3. Giả Lập GPS (Emulator/Debug)
1. Dùng GPS mock tool hoặc Android Studio Location để set tọa độ
2. Set vào: `10.7618166, 106.702175`
3. App sẽ tự động trigger

---

## 🔍 Debug Logs

Khi test, mở **Output** window trong Visual Studio và xem logs:

```
>>> LOCATION UPDATE: 10.761817, 106.702175
--- Geofence Check: 10.761817, 106.702175 ---
POI: Ốc Phát | Distance: 15.23m | Radius: 200m | Priority: 2 | CooldownOK: True
>>> GEOFENCE TRIGGERED: Ốc Phát (Priority: 2)

=== NARRATION SERVICE: Starting for Ốc Phát ===
>>> 🔊 Playing TTS: Ốc Phát
>>> TTS Text: Chào mừng bạn đến với Ốc Phát...
>>> ✅ TTS Completed: Ốc Phát
```

---

## ⚠️ Các Lỗi Thường Gặp

### 1. Không nghe thấy âm thanh
**Nguyên nhân:**
- Âm lượng thiết bị đang tắt/mute
- Quyền RECORD_AUDIO chưa được cấp (Android)
- TTS engine chưa được cài đặt

**Giải pháp:**
- Tăng volume thiết bị
- Kiểm tra quyền trong Settings → Apps → FoodStreetApp
- Cài Google TTS từ Play Store

### 2. Không trigger khi đến gần POI
**Nguyên nhân:**
- GPS chưa chính xác (độ chính xác thấp)
- Cooldown chưa hết (30 giây)
- POI IsActive = false

**Giải pháp:**
- Đợi GPS ổn định (xem accuracy trong logs)
- Đợi 30 giây giữa các lần trigger
- Reset geofence trong POI List page

### 3. Lỗi "No narration configured"
**Nguyên nhân:**
- POI không có TtsText và không có AudioFile

**Giải pháp:**
- Kiểm tra database đã có data chưa
- Xóa app và cài lại để reseed data

---

## 📝 Thông Số Đã Điều Chỉnh (Để Test Dễ Hơn)

| Setting | Giá Trị Cũ | Giá Trị Mới | Mục Đích |
|---------|-----------|-----------|----------|
| Radius | 50m | **200m** | Trigger dễ hơn |
| Cooldown | 300s (5 phút) | **30s** | Test nhanh hơn |
| Update Interval | 5s | 5s | Giữ nguyên |

⚠️ **Lưu ý:** Sau khi test xong, nên đổi lại:
- Radius → 50m (chính xác hơn)
- Cooldown → 300s (tránh spam)

---

## 🎯 Test Cases

### Test Case 1: TTS Test
- [x] Nhấn "Test TTS" → Nghe thấy tiếng nói
- [x] Check Output logs có "Playing TTS"
- [x] Check không có error

### Test Case 2: Narration Service Test
- [x] Nhấn "Test Narration Service"
- [x] Nghe thấy câu test POI
- [x] Check logs có "NARRATION SERVICE: Starting"

### Test Case 3: Geofence Trigger
- [x] Di chuyển vào vùng POI
- [x] Nghe thấy thuyết minh tự động
- [x] UI hiển thị "🎵 Playing: Ốc Phát"
- [x] Logs hiển thị "GEOFENCE TRIGGERED"

### Test Case 4: Cooldown
- [x] Trigger POI lần 1 → Có âm thanh
- [x] Trigger POI lần 2 ngay (< 30s) → Không có
- [x] Đợi 30s → Trigger lại → Có âm thanh

---

## 🚀 Production Recommendations

Khi deploy lên production:

1. **Radius**: Đổi về 30-50m cho chính xác
2. **Cooldown**: Tăng lên 5-10 phút
3. **Priority**: Đảm bảo POI quan trọng có Priority cao hơn
4. **Audio Files**: Thêm file audio thật thay vì chỉ dùng TTS
5. **Battery**: Implement battery optimization
6. **Permissions**: Kiểm tra all permissions trước khi start

---

## 📱 Permissions Cần Thiết

### Android (AndroidManifest.xml)
```xml
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
```

### iOS (Info.plist)
```xml
<key>NSLocationWhenInUseUsageDescription</key>
<string>App cần GPS để phát thuyết minh khi bạn đến gần quán ăn</string>
<key>NSSpeechRecognitionUsageDescription</key>
<string>App cần quyền để phát thuyết minh</string>
```

---

## 💡 Tips

1. **Test trong môi trường thật**: Emulator GPS không chính xác bằng thiết bị thật
2. **Check Output logs**: Mọi action đều có log chi tiết
3. **Reset database**: Nếu có vấn đề, uninstall và cài lại app
4. **TTS language**: Đảm bảo thiết bị có Vietnamese TTS engine

---

**Chúc bạn test thành công! 🎉**
