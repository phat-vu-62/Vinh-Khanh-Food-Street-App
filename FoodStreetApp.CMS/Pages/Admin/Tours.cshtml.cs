using FoodStreetApp.CMS.Interfaces;
using FoodStreetApp.Shared.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodStreetApp.CMS.Pages.Admin;

public class ToursModel : PageModel
{
    private readonly IAdminDataService _service;

    public ToursModel(IAdminDataService service)
    {
        _service = service;
    }

    public IReadOnlyCollection<Tour> Items { get; private set; } = [];
    public IReadOnlyCollection<POI> Pois { get; private set; } = [];

    [BindProperty]
    public TourInput Input { get; set; } = new();

    public void OnGet()
    {
        Items = _service.GetTours();
        Pois = _service.GetPois();
    }

    public IActionResult OnPostAdd()
    {
        _service.AddTour(new Tour
        {
            Name = Input.Name,
            Description = Input.Description,
            PoiIds = ParsePoiIds(Input.PoiIdsCsv),
            IsActive = true
        });

        return RedirectToPage();
    }

    public IActionResult OnPostEdit()
    {
        _service.UpdateTour(new Tour
        {
            Id = Input.Id,
            Name = Input.Name,
            Description = Input.Description,
            PoiIds = ParsePoiIds(Input.PoiIdsCsv),
            IsActive = true
        });

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int id)
    {
        _service.DeleteTour(id);
        return RedirectToPage();
    }

    private static List<int> ParsePoiIds(string poiIdsCsv)
    {
        return poiIdsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var parsed) ? parsed : (int?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
    }

    public class TourInput
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string PoiIdsCsv { get; set; } = string.Empty;
    }
}
