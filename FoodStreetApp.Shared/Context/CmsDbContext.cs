using System.Text.Json;
using FoodStreetApp.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FoodStreetApp.Shared.Context;

public class CmsDbContext : DbContext
{
    public CmsDbContext(DbContextOptions<CmsDbContext> options) : base(options)
    {
    }

    public DbSet<POI> Pois => Set<POI>();
    public DbSet<Audio> Audios => Set<Audio>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Translation> Translations => Set<Translation>();
    public DbSet<UserHistory> UserHistories => Set<UserHistory>();
    public DbSet<PoiSyncAction> PoiSyncActions => Set<PoiSyncAction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var poiIdsConverter = new ValueConverter<List<int>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions?)null) ?? new List<int>());

        var poiIdsComparer = new ValueComparer<List<int>>(
            (c1, c2) => (c1 ?? new List<int>()).SequenceEqual(c2 ?? new List<int>()),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v)),
            c => c.ToList());

        modelBuilder.Entity<Tour>()
            .Property(t => t.PoiIds)
            .HasConversion(poiIdsConverter)
            .Metadata.SetValueComparer(poiIdsComparer);

        modelBuilder.Entity<PoiSyncAction>()
            .Property(x => x.Action)
            .HasMaxLength(20);

        modelBuilder.Entity<PoiSyncAction>()
            .HasIndex(x => x.OccurredAtUtc);

        modelBuilder.Entity<PoiSyncAction>()
            .HasIndex(x => x.PoiId);
    }
}
