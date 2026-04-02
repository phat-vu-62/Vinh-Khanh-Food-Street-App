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
        SslMode = SslMode.Require,
        TrustServerCertificate = true
    };

    connectionString = npgsqlBuilder.ConnectionString;
}

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddDbContext<CmsDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IAdminDataService, AdminDataService>();

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
