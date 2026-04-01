using FoodStreetApp.CMS.Interfaces;
using FoodStreetApp.Shared.Entities;
using FoodStreetApp.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodStreetApp.CMS.Pages.Admin;

public class POIModel : PageModel
{
    private readonly IAdminDataService _service;

    public POIModel(IAdminDataService service)
    {
        _service = service;
    }

    public IReadOnlyCollection<POI> Items { get; private set; } = [];

    [BindProperty]
    public POIInput Input { get; set; } = new();

    public void OnGet()
    {
        Items = _service.GetPois();
    }

    public IActionResult OnPostAdd()
    {
        _service.AddPoi(new POI
        {
            Name = Input.Name,
            Description = Input.Description,
            Latitude = Input.Latitude,
            Longitude = Input.Longitude,
            AudioUrl = Input.AudioUrl,
            Type = POIType.Food,
            IsActive = true
        });

        return RedirectToPage();
    }

    public IActionResult OnPostEdit()
    {
        _service.UpdatePoi(new POI
        {
            Id = Input.Id,
            Name = Input.Name,
            Description = Input.Description,
            Latitude = Input.Latitude,
            Longitude = Input.Longitude,
            AudioUrl = Input.AudioUrl,
            Type = POIType.Food,
            IsActive = true
        });

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int id)
    {
        _service.DeletePoi(id);
        return RedirectToPage();
    }

    public class POIInput
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? AudioUrl { get; set; }
    }
}
