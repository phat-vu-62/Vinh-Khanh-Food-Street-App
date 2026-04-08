using FoodStreetApp.Shared.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = "Host=dpg-d76thjp5pdvs7385s4p0-a.singapore-postgres.render.com;Port=5432;Database=vinhkhanh_db_jjkh;Username=vinhkhanh_db_jjkh_user;Password=eE07cnOuPD9JmF7uWk4mNaI5wvIZRmMv;SSL Mode=Require;Trust Server Certificate=true";

builder.Services.AddDbContext<CmsDbContext>(options => 
    options.UseNpgsql(connectionString, x => x.MigrationsAssembly("FoodStreetApp.CMS")));

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<CmsDbContext>();

Console.WriteLine("Applying manual SQL updates...");

try {
    // We only apply the QRCode update since the others might already exist or fail.
    // We use ExecuteSqlRaw to bypass the EF migration engine.
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"UserHistories\" ADD COLUMN IF NOT EXISTS \"QRCode\" text;");
    
    // We also make sure the migrations history is updated to prevent future conflicts
    await db.Database.ExecuteSqlRawAsync("INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20260408072701_AddQRCodeToUserHistory', '10.0.0') ON CONFLICT DO NOTHING;");
    
    // Mark previous ones as done too if they were pending
    await db.Database.ExecuteSqlRawAsync("INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20260406112234_AddDurationSecondsToUserHistory', '10.0.0') ON CONFLICT DO NOTHING;");
    await db.Database.ExecuteSqlRawAsync("INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20260406114036_AddTextContentToPOI', '10.0.0') ON CONFLICT DO NOTHING;");

    Console.WriteLine("SUCCESS: Database schema updated.");
}
catch (Exception ex) {
    Console.WriteLine($"ERROR: {ex.Message}");
}
