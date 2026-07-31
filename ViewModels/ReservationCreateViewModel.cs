using System.ComponentModel.DataAnnotations;
using CoworkingSpace.Web.Models;

namespace CoworkingSpace.Web.ViewModels;


public class ReservationCreateViewModel
{

    [Display(Name = "Space")]
    public int SpaceId { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    [Display(Name = "Start Time")]
    [DataType(DataType.DateTime)]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "End time is required.")]
    [Display(Name = "End Time")]
    [DataType(DataType.DateTime)]
    public DateTime EndTime { get; set; }

    [DataType(DataType.MultilineText)]
    [StringLength(500)]
    public string? Notes { get; set; }
    public List<object> BusySlots { get; set; } = new List<object>(); // برای نمایش بازه‌های اشغال
    // ===== جدید: لیست فضاهای موجود برای نمایش کارت‌ها =====
    public List<Space> AvailableSpaces { get; set; } = new List<Space>();
}