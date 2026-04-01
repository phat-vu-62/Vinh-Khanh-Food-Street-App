using FoodStreetApp.Shared.Enums;

namespace FoodStreetApp.Shared.Entities;

public class Translation
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public Language Language { get; set; } = Language.Vi;
    public string FieldName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
