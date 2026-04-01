using FoodStreetApp.CMS.Interfaces;
using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.CMS.Services;

public class AdminDataService : IAdminDataService
{
    private readonly List<POI> _pois = new();
    private readonly List<Audio> _audios = new();
    private readonly List<Tour> _tours = new();
    private readonly List<Translation> _translations = new();

    private int _poiId;
    private int _audioId;
    private int _tourId;
    private int _translationId;

    public AdminDataService()
    {
        var poi1 = AddPoi(new POI
        {
            Name = "Ốc Phát",
            Description = "Quán ốc đầu đường Vĩnh Khánh",
            Latitude = 10.761943,
            Longitude = 106.702050,
            AudioUrl = "https://cdn.foodstreet.local/audio/oc-phat.mp3"
        });

        var poi2 = AddPoi(new POI
        {
            Name = "Ốc Hồng Nhung",
            Description = "Quán ốc nổi tiếng đông khách",
            Latitude = 10.761153,
            Longitude = 106.703071,
            AudioUrl = "https://cdn.foodstreet.local/audio/oc-hong-nhung.mp3"
        });

        AddAudio(new Audio
        {
            PoiId = poi1.Id,
            Title = "Narration - Ốc Phát",
            Url = poi1.AudioUrl ?? string.Empty,
            DurationSeconds = 42,
            IsActive = true
        });

        AddAudio(new Audio
        {
            PoiId = poi2.Id,
            Title = "Narration - Ốc Hồng Nhung",
            Url = poi2.AudioUrl ?? string.Empty,
            DurationSeconds = 38,
            IsActive = true
        });

        AddTour(new Tour
        {
            Name = "Vĩnh Khánh Highlights",
            Description = "Top food stops for first-time visitors",
            PoiIds = [poi1.Id, poi2.Id],
            IsActive = true
        });

        AddTranslation(new Translation
        {
            EntityName = "POI",
            EntityId = poi1.Id,
            FieldName = "Description",
            Value = "Fresh snails at the start of Vinh Khanh Street",
            Language = FoodStreetApp.Shared.Enums.Language.En
        });
    }

    public IReadOnlyCollection<POI> GetPois() => _pois.OrderBy(x => x.Id).ToList();

    public POI? GetPoiById(int id) => _pois.FirstOrDefault(x => x.Id == id);

    public POI AddPoi(POI poi)
    {
        poi.Id = ++_poiId;
        _pois.Add(poi);
        return poi;
    }

    public bool UpdatePoi(POI poi)
    {
        var item = GetPoiById(poi.Id);
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
        return true;
    }

    public bool DeletePoi(int id)
    {
        var item = _pois.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        _pois.Remove(item);
        return true;
    }

    public IReadOnlyCollection<Audio> GetAudios() => _audios.OrderBy(x => x.Id).ToList();

    public Audio? GetAudioById(int id) => _audios.FirstOrDefault(x => x.Id == id);

    public Audio AddAudio(Audio audio)
    {
        audio.Id = ++_audioId;
        _audios.Add(audio);
        return audio;
    }

    public bool UpdateAudio(Audio audio)
    {
        var item = GetAudioById(audio.Id);
        if (item is null)
        {
            return false;
        }

        item.PoiId = audio.PoiId;
        item.Title = audio.Title;
        item.Url = audio.Url;
        item.DurationSeconds = audio.DurationSeconds;
        item.IsActive = audio.IsActive;
        return true;
    }

    public bool DeleteAudio(int id)
    {
        var item = _audios.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        _audios.Remove(item);
        return true;
    }

    public IReadOnlyCollection<Tour> GetTours() => _tours.OrderBy(x => x.Id).ToList();

    public Tour? GetTourById(int id) => _tours.FirstOrDefault(x => x.Id == id);

    public Tour AddTour(Tour tour)
    {
        tour.Id = ++_tourId;
        _tours.Add(tour);
        return tour;
    }

    public bool UpdateTour(Tour tour)
    {
        var item = GetTourById(tour.Id);
        if (item is null)
        {
            return false;
        }

        item.Name = tour.Name;
        item.Description = tour.Description;
        item.PoiIds = tour.PoiIds;
        item.IsActive = tour.IsActive;
        return true;
    }

    public bool DeleteTour(int id)
    {
        var item = _tours.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        _tours.Remove(item);
        return true;
    }

    public IReadOnlyCollection<Translation> GetTranslations() => _translations.OrderBy(x => x.Id).ToList();

    public Translation? GetTranslationById(int id) => _translations.FirstOrDefault(x => x.Id == id);

    public Translation AddTranslation(Translation translation)
    {
        translation.Id = ++_translationId;
        _translations.Add(translation);
        return translation;
    }

    public bool UpdateTranslation(Translation translation)
    {
        var item = GetTranslationById(translation.Id);
        if (item is null)
        {
            return false;
        }

        item.EntityName = translation.EntityName;
        item.EntityId = translation.EntityId;
        item.FieldName = translation.FieldName;
        item.Value = translation.Value;
        item.Language = translation.Language;
        return true;
    }

    public bool DeleteTranslation(int id)
    {
        var item = _translations.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        _translations.Remove(item);
        return true;
    }
}
