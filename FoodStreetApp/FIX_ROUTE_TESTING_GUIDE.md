# 🔧 Hướng Dẫn Test App với Mock Location Routes

## ✅ Đã Khắc Phục

### 1. **Thêm nút Center to Location (🎯)**
- Nút này nằm ở **góc dưới bên phải** trên MapPage
- Click vào nút này để **tự động nhảy đến vị trí hiện tại** của bạn
- Zoom radius: 200m

### 2. **Sửa Geofence để hỗ trợ Mock Location Test**
- Trước đây: Chỉ trigger khi **đang approaching** (distance giảm dần)
- Bây giờ: Trigger khi **vừa entering radius** hoặc **approaching**
- Phù hợp cho cả **real GPS** và **mock location jumps**

---

## 📱 Cách Test với Mock Location Routes

### Bước 1: Bật Developer Options trên Android
1. Settings → About Phone → Tap "Build Number" 7 lần
2. Quay lại Settings → System → Developer Options
3. Bật **"Allow mock locations"**

### Bước 2: Cài đặt Mock Location App
Khuyên dùng: **GPS JoyStick** hoặc **Fake GPS location**

### Bước 3: Thiết lập Route Test
1. Mở Mock Location App
2. Chọn **"Route"** mode
3. Vẽ route đi qua các POI của bạn trên Vinh Khanh
4. Tốc độ: 5-10 km/h (tốc độ đi bộ)
5. Start Route

### Bước 4: Test App
1. Mở Food Street App
2. Vào **Tab "Bản đồ"**
3. Click nút **"🔄 Reset All"** để reset tất cả cooldown
4. Start Route trong Mock Location App
5. Quan sát:
   - Location dot trên map di chuyển theo route
   - Khi vào radius của POI → **Tự động phát thuyết minh**
   - Status panel hiển thị "Playing narration: [POI Name]"

---

## 🐛 Debug Tips

### Nếu vẫn không nghe thuyết minh:

1. **Check Debug Log:**
   ```
   - ">>> 🎯 APPROACHING POI: [Name]" → OK
   - "FirstEntry: True" → Đã vào radius
   - "CooldownOK: True" → Cooldown đã hết
   ```

2. **Kiểm tra Settings:**
   - Vào Tab "Cài đặt"
   - Bật **"Enable Narration"**
   - Bật **"Use Audio Files"** nếu có file audio
   - Chọn đúng **Language** (Tiếng Việt)

3. **Test TTS riêng:**
   - Vào Settings
   - Click "Test Narration"
   - Nghe có phát ra âm thanh không?

4. **Check POI Radius:**
   - Vào Tab "Danh sách"
   - Click vào POI để xem radius
   - Đảm bảo mock location đi **VÀO TRONG radius**

5. **Reset Geofences:**
   - Click nút "🔄 Reset All" trước mỗi lần test
   - Hoặc đợi cooldown (30-60s mặc định)

---

## 📊 Cách Route Test Hoạt Động

### Before Fix:
```
Mock Location Jump: A (500m) → B (10m) → C (500m)
             ^            ^          ^
           Outside      Inside     Outside
                        
Approaching? ❌ NO (nhảy vọt)
→ Không trigger thuyết minh
```

### After Fix:
```
Mock Location Jump: A (500m) → B (10m) → C (500m)
             ^            ^          ^
           Outside      Inside     Outside
                     
FirstEntry? ✅ YES (vừa vào radius)
→ Trigger thuyết minh ngay lập tức!
```

---

## 🎯 Expected Behavior

Khi bạn test route qua các POI:

1. **Vào radius POI đầu tiên:**
   - ✅ Phát thuyết minh ngay lập tức
   - Status: "Playing narration: [POI Name]"
   - Cooldown bắt đầu

2. **Ra khỏi và vào lại POI đó:**
   - ⏳ Nếu chưa hết cooldown: Không phát
   - ✅ Nếu đã hết cooldown: Phát lại

3. **Vào radius POI khác:**
   - ✅ Phát thuyết minh mới
   - Priority cao → Priority thấp

4. **Nút Center Location (🎯):**
   - Click → Nhảy về vị trí hiện tại
   - Map tự động zoom vào radius 200m

---

## 🔧 Troubleshooting

### Mock location không di chuyển:
- Đảm bảo app có quyền "Mock Location"
- Restart app sau khi set mock location

### Thuyết minh phát nhưng không có âm thanh:
- Check volume điện thoại
- Test TTS trong Settings
- Kiểm tra language match với TTS engine

### App crash khi start route:
- Check logcat: `adb logcat | grep "LocationService"`
- Đảm bảo có quyền ACCESS_FINE_LOCATION

---

## ✨ Summary

**Đã sửa:**
1. ✅ Thêm nút Center to Location (🎯)
2. ✅ Sửa GeofenceService hỗ trợ mock location jumps
3. ✅ Trigger thuyết minh khi **firstEntry** hoặc **approaching**

**Cách test:**
1. Reset All
2. Start mock location route
3. Đi qua POI → Nghe thuyết minh tự động
4. Click 🎯 để quay về vị trí hiện tại

Giờ app đã hoạt động tốt với cả **real GPS** và **mock location routes**! 🎉
