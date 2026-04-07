using FoodStreetApp.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace FoodStreetApp.CMS.ViewModels;

public enum AudioSourceMode { File, TTS }

public class PoiFormViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Tên địa điểm là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên không được quá 100 ký tự")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Mô tả ngắn là bắt buộc")]
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "Vĩ độ là bắt buộc")]
    [Range(-90, 90, ErrorMessage = "Vĩ độ không hợp lệ")]
    public double Latitude { get; set; }
    
    [Required(ErrorMessage = "Kinh độ là bắt buộc")]
    [Range(-180, 180, ErrorMessage = "Kinh độ không hợp lệ")]
    public double Longitude { get; set; }
    
    public string? AudioUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int RadiusMeters { get; set; } = 15;
    public POIType Type { get; set; } = POIType.Food;

    // SaaS Enhancements
    public AudioSourceMode AudioMode { get; set; } = AudioSourceMode.File;
    public string? ImageUrl { get; set; }

    // Translation fields
    public string? NameVi { get; set; }
    public string? NameEn { get; set; }
    public string? NameZh { get; set; }
    public string? NameKo { get; set; }
    public string? NameJa { get; set; }

    public string? DescriptionVi { get; set; }
    public string? DescriptionEn { get; set; }
    public string? DescriptionZh { get; set; }
    public string? DescriptionKo { get; set; }
    public string? DescriptionJa { get; set; }

    public string? TextContent { get; set; }
    public string? TextContentVi { get; set; }
    public string? TextContentEn { get; set; }
    public string? TextContentZh { get; set; }
    public string? TextContentKo { get; set; }
    public string? TextContentJa { get; set; }
}
