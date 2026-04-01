namespace FoodStreetApp.CMS.Models;

public class PageHeaderModel
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? ActionText { get; set; }
    public string? ActionLink { get; set; }
}
