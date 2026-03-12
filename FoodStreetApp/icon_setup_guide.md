# Tab Icons Setup

## Required Icons

Để hiển thị icons cho 3 tabs trong AppShell, bạn cần thêm các file icon sau vào thư mục `Resources/Images/`:

1. **map.png** - Icon cho tab "Bản đồ"
2. **list.png** - Icon cho tab "Danh sách"  
3. **settings.png** - Icon cho tab "Cài đặt"

## Tạm thời bỏ icons trong AppShell.xaml

Xóa các dòng `Icon="..."` trong file `AppShell.xaml` để app chạy được:

```xml
<TabBar>
    <ShellContent
        Title="Bản đồ"
        ContentTemplate="{DataTemplate views:MapPage}"
        Route="MapPage" />

    <ShellContent
        Title="Danh sách"
        ContentTemplate="{DataTemplate views:POIListPage}"
        Route="POIListPage" />

    <ShellContent
        Title="Cài đặt"
        ContentTemplate="{DataTemplate views:SettingsPage}"
        Route="SettingsPage" />
</TabBar>
```
