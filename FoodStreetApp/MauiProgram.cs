using Microsoft.Extensions.Logging;
using FoodStreetApp.Services;
using FoodStreetApp.Views;
using FoodStreetApp.ViewModels;
using FoodStreetApp.Data;
using Plugin.Maui.Audio;

namespace FoodStreetApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Audio
            builder.Services.AddSingleton(AudioManager.Current);

            // Data
            builder.Services.AddSingleton<IPOIRepository, POIRepository>();

            // Services
            builder.Services.AddSingleton<ILocationService, LocationService>();
            builder.Services.AddSingleton<IAudioService, AudioService>();
            builder.Services.AddSingleton<INarrationService, NarrationService>();
            builder.Services.AddSingleton<IPOIService, POIService>();
            builder.Services.AddSingleton<IGeofenceService, GeofenceService>();

            // ViewModels & Views
            builder.Services.AddTransient<HomeViewModel>();
            builder.Services.AddTransient<HomePage>();

            builder.Services.AddTransient<FoodDetailViewModel>();
            builder.Services.AddTransient<FoodDetailPage>();

            builder.Services.AddTransient<MapPageViewModel>();
            builder.Services.AddTransient<MapPage>();

            builder.Services.AddTransient<POIListViewModel>();
            builder.Services.AddTransient<POIListPage>();

            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<SettingsPage>();

            return builder.Build();
        }
    }
}
