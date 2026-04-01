using FoodStreetApp.CMS.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodStreetApp.CMS.Pages;

public class IndexModel : PageModel
{
    private readonly IAdminDataService _service;

    public IndexModel(IAdminDataService service)
    {
        _service = service;
    }

    public int TotalPois { get; private set; }
    public int TotalTours { get; private set; }
    public int TotalAudios { get; private set; }

    public void OnGet()
    {
        TotalPois = _service.GetPois().Count;
        TotalTours = _service.GetTours().Count;
        TotalAudios = _service.GetAudios().Count;
    }
}
