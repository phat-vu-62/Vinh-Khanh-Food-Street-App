using FoodStreetApp.CMS.Interfaces;
using FoodStreetApp.CMS.Data;
using FoodStreetApp.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodStreetApp.CMS.Services;

public class AdminDataService : IAdminDataService
{
    private readonly CmsDbContext _dbContext;

    public AdminDataService(CmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyCollection<POI> GetPois() => _dbContext.Pois.OrderBy(x => x.Id).ToList();

    public POI? GetPoiById(int id) => _dbContext.Pois.FirstOrDefault(x => x.Id == id);

    public POI AddPoi(POI poi)
    {
        _dbContext.Pois.Add(poi);
        _dbContext.SaveChanges();
        return poi;
    }

    public bool UpdatePoi(POI poi)
    {
        var item = _dbContext.Pois.FirstOrDefault(x => x.Id == poi.Id);
        if (item is null)
        {
            return false;
        }

        item.Name = poi.Name;
        item.Description = poi.Description;
        item.Latitude = poi.Latitude;
        item.Longitude = poi.Longitude;
        item.AudioUrl = poi.AudioUrl;
        item.IsActive = poi.IsActive;
        item.Type = poi.Type;
        item.RadiusMeters = poi.RadiusMeters;
        _dbContext.SaveChanges();
        return true;
    }

    public bool DeletePoi(int id)
    {
        var item = _dbContext.Pois.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        _dbContext.Pois.Remove(item);
        _dbContext.SaveChanges();
        return true;
    }

    public IReadOnlyCollection<Audio> GetAudios() => _dbContext.Audios.OrderBy(x => x.Id).ToList();

    public Audio? GetAudioById(int id) => _dbContext.Audios.FirstOrDefault(x => x.Id == id);

    public Audio AddAudio(Audio audio)
    {
        _dbContext.Audios.Add(audio);
        _dbContext.SaveChanges();
        return audio;
    }

    public bool UpdateAudio(Audio audio)
    {
        var item = _dbContext.Audios.FirstOrDefault(x => x.Id == audio.Id);
        if (item is null)
        {
            return false;
        }

        item.PoiId = audio.PoiId;
        item.Title = audio.Title;
        item.Url = audio.Url;
        item.DurationSeconds = audio.DurationSeconds;
        item.IsActive = audio.IsActive;
        _dbContext.SaveChanges();
        return true;
    }

    public bool DeleteAudio(int id)
    {
        var item = _dbContext.Audios.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        _dbContext.Audios.Remove(item);
        _dbContext.SaveChanges();
        return true;
    }

    public IReadOnlyCollection<Tour> GetTours() => _dbContext.Tours.AsNoTracking().OrderBy(x => x.Id).ToList();

    public Tour? GetTourById(int id) => _dbContext.Tours.FirstOrDefault(x => x.Id == id);

    public Tour AddTour(Tour tour)
    {
        _dbContext.Tours.Add(tour);
        _dbContext.SaveChanges();
        return tour;
    }

    public bool UpdateTour(Tour tour)
    {
        var item = _dbContext.Tours.FirstOrDefault(x => x.Id == tour.Id);
        if (item is null)
        {
            return false;
        }

        item.Name = tour.Name;
        item.Description = tour.Description;
        item.PoiIds = tour.PoiIds;
        item.IsActive = tour.IsActive;
        _dbContext.SaveChanges();
        return true;
    }

    public bool DeleteTour(int id)
    {
        var item = _dbContext.Tours.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        _dbContext.Tours.Remove(item);
        _dbContext.SaveChanges();
        return true;
    }

    public IReadOnlyCollection<Translation> GetTranslations() => _dbContext.Translations.OrderBy(x => x.Id).ToList();

    public Translation? GetTranslationById(int id) => _dbContext.Translations.FirstOrDefault(x => x.Id == id);

    public Translation AddTranslation(Translation translation)
    {
        _dbContext.Translations.Add(translation);
        _dbContext.SaveChanges();
        return translation;
    }

    public bool UpdateTranslation(Translation translation)
    {
        var item = _dbContext.Translations.FirstOrDefault(x => x.Id == translation.Id);
        if (item is null)
        {
            return false;
        }

        item.EntityName = translation.EntityName;
        item.EntityId = translation.EntityId;
        item.FieldName = translation.FieldName;
        item.Value = translation.Value;
        item.Language = translation.Language;
        _dbContext.SaveChanges();
        return true;
    }

    public bool DeleteTranslation(int id)
    {
        var item = _dbContext.Translations.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        _dbContext.Translations.Remove(item);
        _dbContext.SaveChanges();
        return true;
    }
}
