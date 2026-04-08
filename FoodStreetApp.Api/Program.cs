using FoodStreetApp.Api.Interfaces;
using FoodStreetApp.Api.Repositories;
using FoodStreetApp.Api.Services;
using FoodStreetApp.Shared.Entities;
using FoodStreetApp.Shared.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DB Context Setup
var connectionString = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<CmsDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

builder.Services.AddScoped<IPOIService, POIService>();
builder.Services.AddScoped<ITourService, TourService>();
builder.Services.AddScoped<IAudioService, AudioService>();

builder.Services.AddHttpClient<IGeminiTranslationService, GeminiTranslationService>();

builder.Services.AddSingleton<IGeofenceService, GeofenceService>();
builder.Services.AddSingleton<INarrationEngine, NarrationEngine>();
builder.Services.AddScoped<IQRCodeService, QRCodeService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())

{
    app.UseHttpsRedirection();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "FoodStreetApp.Api",
    status = "running",
    endpoints = new[] { "/api/POI", "/api/Tour", "/api/Audio", "/qr/{poiId}" }
}));

// QR code deep link endpoint
app.MapGet("/qr/{poiId}", (int poiId) => 
{
    // Generate an endpoint that allows user to open the mobile app directly.
    // In a real scenario, this might return an HTML page with branch links,
    // or return a redirect to a custom scheme for the MAUI app.
    string deepLink = $"foodstreet://poi?id={poiId}&play=true&skipGps=true";
    
    // Simple HTML redirect page
    string html = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta name='viewport' content='width=device-width, initial-scale=1'>
        <title>Opening FoodStreet...</title>
        <meta http-equiv='refresh' content='0;url={deepLink}'>
    </head>
    <body style='text-align: center; padding: 50px; font-family: sans-serif;'>
        <h3>Redirecting to FoodStreet App...</h3>
        <p>If you have the app installed, it should open automatically.</p>
        <a href='{deepLink}' style='padding: 10px 20px; background: #007bff; color: white; text-decoration: none; border-radius: 5px;'>Open App Manually</a>
    </body>
    </html>";
    
    return Results.Content(html, "text/html");
});

app.MapControllers();

app.Run();
