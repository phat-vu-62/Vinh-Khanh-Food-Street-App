using System.Net.Http.Json;
using FoodStreetApp.Shared.Entities;

namespace FoodStreetApp.CMS.Services;

public class CmsApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CmsApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient Client => _httpClientFactory.CreateClient("API");

    public async Task<IReadOnlyCollection<POI>> GetPoisAsync(CancellationToken cancellationToken = default)
        => await Client.GetFromJsonAsync<List<POI>>("api/POI", cancellationToken) ?? [];

    public async Task<POI?> CreatePoiAsync(POI poi, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("api/POI", poi, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<POI>(cancellationToken: cancellationToken);
    }

    public async Task UpdatePoiAsync(POI poi, CancellationToken cancellationToken = default)
    {
        var response = await Client.PutAsJsonAsync($"api/POI/{poi.Id}", poi, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeletePoiAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await Client.DeleteAsync($"api/POI/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyCollection<Audio>> GetAudiosAsync(CancellationToken cancellationToken = default)
        => await Client.GetFromJsonAsync<List<Audio>>("api/Audio", cancellationToken) ?? [];

    public async Task<Audio?> CreateAudioAsync(Audio audio, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("api/Audio", audio, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Audio>(cancellationToken: cancellationToken);
    }

    public async Task UpdateAudioAsync(Audio audio, CancellationToken cancellationToken = default)
    {
        var response = await Client.PutAsJsonAsync($"api/Audio/{audio.Id}", audio, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAudioAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await Client.DeleteAsync($"api/Audio/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyCollection<Tour>> GetToursAsync(CancellationToken cancellationToken = default)
        => await Client.GetFromJsonAsync<List<Tour>>("api/Tour", cancellationToken) ?? [];

    public async Task<Tour?> CreateTourAsync(Tour tour, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("api/Tour", tour, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Tour>(cancellationToken: cancellationToken);
    }

    public async Task UpdateTourAsync(Tour tour, CancellationToken cancellationToken = default)
    {
        var response = await Client.PutAsJsonAsync($"api/Tour/{tour.Id}", tour, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteTourAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await Client.DeleteAsync($"api/Tour/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
