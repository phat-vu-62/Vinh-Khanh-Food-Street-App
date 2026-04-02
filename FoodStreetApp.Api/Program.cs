using FoodStreetApp.Api.Interfaces;
using FoodStreetApp.Api.Repositories;
using FoodStreetApp.Api.Services;
using FoodStreetApp.Shared.Entities;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers();

builder.Services.AddSingleton<IRepository<POI>>(_ => new InMemoryRepository<POI>(x => x.Id, (x, id) => x.Id = id));
builder.Services.AddSingleton<IRepository<Tour>>(_ => new InMemoryRepository<Tour>(x => x.Id, (x, id) => x.Id = id));
builder.Services.AddSingleton<IRepository<Audio>>(_ => new InMemoryRepository<Audio>(x => x.Id, (x, id) => x.Id = id));

builder.Services.AddScoped<IPOIService, POIService>();
builder.Services.AddScoped<ITourService, TourService>();
builder.Services.AddScoped<IAudioService, AudioService>();

builder.Services.AddHttpClient<IGeminiTranslationService, GeminiTranslationService>();

builder.Services.AddSingleton<IGeofenceService, GeofenceService>();
builder.Services.AddSingleton<INarrationEngine, NarrationEngine>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "FoodStreetApp.Api",
    status = "running",
    endpoints = new[] { "/api/POI", "/api/Tour", "/api/Audio" }
}));

app.MapControllers();

app.Run();
