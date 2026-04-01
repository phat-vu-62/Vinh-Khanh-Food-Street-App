using FoodStreetApp.CMS.Interfaces;
using FoodStreetApp.Shared.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodStreetApp.CMS.Pages.Admin;

public class AudioModel : PageModel
{
    private readonly IAdminDataService _service;

    public AudioModel(IAdminDataService service)
    {
        _service = service;
    }

    public IReadOnlyCollection<Audio> Items { get; private set; } = [];

    [BindProperty]
    public AudioInput Input { get; set; } = new();

    public void OnGet()
    {
        Items = _service.GetAudios();
    }

    public IActionResult OnPostAdd()
    {
        _service.AddAudio(new Audio
        {
            PoiId = Input.PoiId,
            Title = Input.Title,
            Url = Input.Url,
            DurationSeconds = Input.DurationSeconds,
            IsActive = true
        });

        return RedirectToPage();
    }

    public IActionResult OnPostEdit()
    {
        _service.UpdateAudio(new Audio
        {
            Id = Input.Id,
            PoiId = Input.PoiId,
            Title = Input.Title,
            Url = Input.Url,
            DurationSeconds = Input.DurationSeconds,
            IsActive = true
        });

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int id)
    {
        _service.DeleteAudio(id);
        return RedirectToPage();
    }

    public class AudioInput
    {
        public int Id { get; set; }
        public int PoiId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
    }
}
