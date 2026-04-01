using FoodStreetApp.Shared.Enums;

namespace FoodStreetApp.Shared.DTOs;

public class TranslationDto
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public Language Language { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
