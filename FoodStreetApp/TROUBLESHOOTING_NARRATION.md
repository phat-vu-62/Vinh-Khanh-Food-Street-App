# ✅ Checklist Khắc Phục "Không Nghe Thấy Thuyết Minh"

## 🔧 Các Bước Khắc Phục Nhanh

### Bước 1: Kiểm Tra Cơ Bản
- [ ] Âm lượng thiết bị đã bật chưa?
- [ ] Có đang ở chế độ im lặng/rung không?
- [ ] Thiết bị có lỗi phần cứng audio không?

### Bước 2: Test Ngay Trong App
1. [ ] Mở app → Vào **Settings**
2. [ ] Nhấn nút **"🔊 Test TTS"**
3. [ ] Có nghe thấy không?
   - ✅ **CÓ** → TTS hoạt động, vấn đề ở geofence/GPS
   - ❌ **KHÔNG** → Vấn đề TTS, xem bước 3

### Bước 3: Kiểm Tra TTS Engine (Nếu không nghe thấy)
**Android:**
- [ ] Vào Settings → Language & Input → Text-to-speech
- [ ] Kiểm tra có **Google Text-to-Speech** chưa
- [ ] Download Vietnamese language pack
- [ ] Test TTS trong system settings

**iOS:**
- [ ] Settings → Accessibility → Spoken Content
- [ ] Bật "Speak Selection"
- [ ] Test với Safari reader mode

### Bước 4: Kiểm Tra Quyền
**Android:**
- [ ] Settings → Apps → FoodStreetApp → Permissions
- [ ] Location: **Allow all the time** (hoặc When using)
- [ ] Microphone: **Allow** (cần cho TTS trên một số thiết bị)

**iOS:**
- [ ] Settings → Privacy → Location Services → FoodStreetApp
- [ ] Chọn **While Using the App**
- [ ] Bật **Precise Location**

### Bước 5: Test GPS & Geofence
1. [ ] Mở **Visual Studio Output** window
2. [ ] Chạy app trong debug mode
3. [ ] Vào Map page
4. [ ] Xem logs có hiển thị:
   ```
   >>> LOCATION UPDATE: [latitude], [longitude]
   --- Geofence Check: ...
   ```
5. [ ] Nếu KHÔNG có logs → GPS không hoạt động

### Bước 6: Mock Location (Test Nhanh)
**Android:**
1. [ ] Enable Developer Options
2. [ ] Settings → Developer → Select mock location app
3. [ ] Dùng app như "Fake GPS Location"
4. [ ] Set tọa độ: `10.7618166, 106.702175`
5. [ ] Mở app → Xem có trigger không

### Bước 7: Kiểm Tra Database
Xem logs có dòng này không:
```
Database initialized at: [path]
GeofenceService initialized with X POIs
```
- [ ] Nếu KHÔNG → Database không load được
- [ ] Giải pháp: Uninstall app → Cài lại

### Bước 8: Reset Geofence
1. [ ] Vào POI List page
2. [ ] Tìm POI "Ốc Phát"
3. [ ] Nhấn nút Reset (nếu có)
4. [ ] Test lại

---

## 🐛 Debug Commands

### Xem Logs Chi Tiết
Trong Visual Studio Output window, tìm:

**✅ Logs Tốt (Working):**
```
>>> LOCATION UPDATE: 10.761817, 106.702175
POI: Ốc Phát | Distance: 45.23m | Radius: 200m | CooldownOK: True
>>> GEOFENCE TRIGGERED: Ốc Phát
>>> 🔊 Playing TTS: Ốc Phát
>>> ✅ TTS Completed: Ốc Phát
```

**❌ Logs Lỗi:**
```
>>> ❌ ERROR playing narration: ...
Unable to get location: ...
```

---

## 🎯 Quick Test Flow

### Test 1: TTS Trực Tiếp (30 giây)
```
Settings → Test TTS button → Nghe thấy?
```
- ✅ CÓ → TTS OK
- ❌ KHÔNG → Fix TTS engine

### Test 2: Narration Service (30 giây)
```
Settings → Test Narration Service → Nghe thấy?
```
- ✅ CÓ → Service OK
- ❌ KHÔNG → Check logs

### Test 3: Geofence (2 phút)
```
Map page → Mock GPS (10.7618166, 106.702175) → Đợi 10s → Nghe thấy?
```
- ✅ CÓ → Everything works!
- ❌ KHÔNG → Check logs geofence

---

## 🔥 Giải Pháp Nhanh Nhất

**Nếu vẫn không được:**

### Option 1: Clean Install
```bash
# Xóa app khỏi thiết bị
# Build lại trong Visual Studio
# Deploy lại
# Test ngay với Test TTS button
```

### Option 2: Tăng Logging
Đã được thêm sẵn! Chỉ cần:
1. Run app trong Debug mode
2. Mở Output window
3. Làm theo test cases
4. Share logs nếu vẫn lỗi

### Option 3: Test Settings
Đã có test buttons trong Settings page:
- 🔊 Test TTS
- 🎵 Test Narration Service

→ Dùng ngay không cần di chuyển!

---

## 📞 Báo Lỗi

Nếu vẫn không được, cung cấp:
1. Device: (Android/iOS, version?)
2. Output logs: (copy từ VS Output)
3. Test TTS button result: (có nghe không?)
4. GPS accuracy: (xem trong logs)
5. Screenshot app nếu có thể

---

**🎉 Hầu hết trường hợp sẽ OK sau khi test với Test TTS button!**
