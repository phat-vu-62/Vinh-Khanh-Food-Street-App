using FoodStreetApp.CMS.Interfaces;
using FoodStreetApp.CMS.Data;
using FoodStreetApp.CMS.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var connectionString = builder.Configuration.GetConnectionString("Postgres");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL")
    ?? builder.Configuration["Api:BaseUrl"]
    ?? "https://vinh-khanh-food-street-app.onrender.com/";

if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    var databaseUri = new Uri(databaseUrl);
    var userInfo = databaseUri.UserInfo.Split(':', 2);

    var npgsqlBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = databaseUri.Host,
        Port = databaseUri.Port,
        Database = databaseUri.AbsolutePath.TrimStart('/'),
        Username = userInfo[0],
        Password = userInfo.Length > 1 ? userInfo[1] : string.Empty,
        SslMode = SslMode.Require
    };

    connectionString = npgsqlBuilder.ConnectionString;
}

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddDbContext<CmsDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IAdminDataService, AdminDataService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddScoped<CmsApiService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
    dbContext.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapGet("/api/POI", (IAdminDataService service) => Results.Ok(service.GetPois()));
app.MapGet("/api/POI/{id:int}", (int id, IAdminDataService service) =>
{
    var item = service.GetPoiById(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
});
app.MapPost("/api/POI", (FoodStreetApp.Shared.Entities.POI poi, IAdminDataService service) =>
{
    var created = service.AddPoi(poi);
    return Results.Created($"/api/POI/{created.Id}", created);
});
app.MapPut("/api/POI/{id:int}", (int id, FoodStreetApp.Shared.Entities.POI poi, IAdminDataService service) =>
{
    poi.Id = id;
    return service.UpdatePoi(poi) ? Results.NoContent() : Results.NotFound();
});
app.MapDelete("/api/POI/{id:int}", (int id, IAdminDataService service) =>
    service.DeletePoi(id) ? Results.NoContent() : Results.NotFound());

app.MapGet("/api/Audio", (IAdminDataService service) => Results.Ok(service.GetAudios()));
app.MapGet("/api/Audio/{id:int}", (int id, IAdminDataService service) =>
{
    var item = service.GetAudioById(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
});
app.MapPost("/api/Audio", (FoodStreetApp.Shared.Entities.Audio audio, IAdminDataService service) =>
{
    var created = service.AddAudio(audio);
    return Results.Created($"/api/Audio/{created.Id}", created);
});
app.MapPut("/api/Audio/{id:int}", (int id, FoodStreetApp.Shared.Entities.Audio audio, IAdminDataService service) =>
{
    audio.Id = id;
    return service.UpdateAudio(audio) ? Results.NoContent() : Results.NotFound();
});
app.MapDelete("/api/Audio/{id:int}", (int id, IAdminDataService service) =>
    service.DeleteAudio(id) ? Results.NoContent() : Results.NotFound());

app.MapGet("/api/Tour", (IAdminDataService service) => Results.Ok(service.GetTours()));
app.MapGet("/api/Tour/{id:int}", (int id, IAdminDataService service) =>
{
    var item = service.GetTourById(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
});
app.MapPost("/api/Tour", (FoodStreetApp.Shared.Entities.Tour tour, IAdminDataService service) =>
{
    var created = service.AddTour(tour);
    return Results.Created($"/api/Tour/{created.Id}", created);
});
app.MapPut("/api/Tour/{id:int}", (int id, FoodStreetApp.Shared.Entities.Tour tour, IAdminDataService service) =>
{
    tour.Id = id;
    return service.UpdateTour(tour) ? Results.NoContent() : Results.NotFound();
});
app.MapDelete("/api/Tour/{id:int}", (int id, IAdminDataService service) =>
    service.DeleteTour(id) ? Results.NoContent() : Results.NotFound());

app.MapGet("/api/sync/pois", (IAdminDataService service) =>
{
    var audios = service.GetAudios()
        .Where(x => x.IsActive)
        .GroupBy(x => x.PoiId)
        .ToDictionary(g => g.Key, g => g.First().Url);

    var translations = service.GetTranslations()
        .Where(t => string.Equals(t.EntityName, "POI", StringComparison.OrdinalIgnoreCase))
        .ToList();

    var result = service.GetPois()
        .OrderByDescending(p => p.Id)
        .Select(p =>
        {
            var viTts = translations
                .LastOrDefault(t => t.EntityId == p.Id && t.Language == FoodStreetApp.Shared.Enums.Language.Vi && t.FieldName == "TtsText")
                ?.Value ?? p.Description ?? p.Name;

            var enTts = translations
                .LastOrDefault(t => t.EntityId == p.Id && t.Language == FoodStreetApp.Shared.Enums.Language.En && t.FieldName == "TtsText")
                ?.Value ?? p.Description ?? p.Name;

            audios.TryGetValue(p.Id, out var audioUrl);

            return new
            {
                p.Id,
                p.Name,
                p.Latitude,
                p.Longitude,
                Radius = (double)p.RadiusMeters,
                ApproachRadius = 200d,
                Priority = p.Id,
                Rating = 4.5d,
                ReviewCount = 100,
                Description = p.Description ?? string.Empty,
                AudioFile = audioUrl ?? p.AudioUrl ?? string.Empty,
                TtsText = viTts,
                TtsTextEn = enTts,
                TtsTextKo = string.Empty,
                TtsTextZh = string.Empty,
                TtsTextJa = string.Empty,
                UseTts = true,
                CooldownSeconds = 60,
                p.IsActive
            };
        })
        .ToList();

    return Results.Ok(result);
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
