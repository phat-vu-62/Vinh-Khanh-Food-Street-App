using FoodStreetApp.CMS.Interfaces;
using FoodStreetApp.Shared.Entities;
using FoodStreetApp.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodStreetApp.CMS.Pages.Admin;

public class TranslationsModel : PageModel
{
    private readonly IAdminDataService _service;

    public TranslationsModel(IAdminDataService service)
    {
        _service = service;
    }

    public IReadOnlyCollection<Translation> Items { get; private set; } = [];
    public IReadOnlyCollection<POI> Pois { get; private set; } = [];

    [BindProperty]
    public TranslationInput Input { get; set; } = new();

    public void OnGet()
    {
        Items = _service.GetTranslations();
        Pois = _service.GetPois();
    }

    public IActionResult OnPostAdd()
    {
        _service.AddTranslation(new Translation
        {
            EntityName = string.IsNullOrWhiteSpace(Input.EntityName) ? "POI" : Input.EntityName,
            EntityId = Input.EntityId,
            FieldName = Input.FieldName,
            Value = Input.Value,
            Language = Input.Language
        });

        return RedirectToPage();
    }

    public IActionResult OnPostEdit()
    {
        _service.UpdateTranslation(new Translation
        {
            Id = Input.Id,
            EntityName = string.IsNullOrWhiteSpace(Input.EntityName) ? "POI" : Input.EntityName,
            EntityId = Input.EntityId,
            FieldName = Input.FieldName,
            Value = Input.Value,
            Language = Input.Language
        });

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int id)
    {
        _service.DeleteTranslation(id);
        return RedirectToPage();
    }

    public class TranslationInput
    {
        public int Id { get; set; }
        public string EntityName { get; set; } = "POI";
        public int EntityId { get; set; }
        public Language Language { get; set; } = Language.Vi;
        public string FieldName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
