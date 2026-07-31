using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoworkingSpace.Web.Models;

public class SpaceImage
{
    [Key]
    public int ImageId { get; set; }

    [Required]
    [ForeignKey(nameof(Space))]
    public int SpaceId { get; set; }

    [Required]
    [StringLength(500)]
    public string ImagePath { get; set; } = string.Empty; // مثلاً: "/images/spaces/space1_1.jpg"

    public int DisplayOrder { get; set; } = 0;

    public bool IsPrimary { get; set; } = false;

    // ===== Navigation Property =====
    public virtual Space? Space { get; set; }
}