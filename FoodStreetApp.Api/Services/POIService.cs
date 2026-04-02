using FoodStreetApp.Api.Interfaces;
using FoodStreetApp.Api.Repositories;
using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.Api.Services;

public class POIService : IPOIService
{
    private readonly IRepository<POI> _repository;
    private readonly IGeminiTranslationService _translationService;

    public POIService(IRepository<POI> repository, IGeminiTranslationService translationService)
    {
        _repository = repository;
        _translationService = translationService;
    }

    public Task<IReadOnlyCollection<POI>> GetAllAsync() => _repository.GetAllAsync();

    public Task<POI?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public async Task<POI> CreateAsync(POI poi)
    {
        await _translationService.TranslatePoiAsync(poi);
        return await _repository.CreateAsync(poi);
    }

    public async Task<bool> UpdateAsync(int id, POI poi)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return false;

        bool contentChanged = existing.Name != poi.Name || existing.Description != poi.Description;
        bool translationsMissing = string.IsNullOrWhiteSpace(existing.NameEn) || 
                                   string.IsNullOrWhiteSpace(existing.DescriptionEn);

        if (contentChanged || translationsMissing)
        {
            await _translationService.TranslatePoiAsync(poi);
        }
        else
        {
            poi.NameVi = existing.NameVi;
            poi.NameEn = existing.NameEn;
            poi.NameZh = existing.NameZh;
            poi.NameKo = existing.NameKo;
            poi.NameJa = existing.NameJa;
            
            poi.DescriptionVi = existing.DescriptionVi;
            poi.DescriptionEn = existing.DescriptionEn;
            poi.DescriptionZh = existing.DescriptionZh;
            poi.DescriptionKo = existing.DescriptionKo;
            poi.DescriptionJa = existing.DescriptionJa;
        }

        return await _repository.UpdateAsync(id, poi);
    }

    public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);
}
