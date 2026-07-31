using System.ComponentModel.DataAnnotations;

namespace CoworkingSpace.Web.Models;

/// <summary>
/// Inventory item (projector, whiteboard, etc.) that can be assigned to spaces.
/// Maps to table <c>Equipment</c>.
/// </summary>
public class Equipment
{
    [Column("equipment_id")]
    public int EquipmentId { get; set; }

    [Column("name", TypeName = "nvarchar(100)")]
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("description", TypeName = "nvarchar(500)")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("category", TypeName = "nvarchar(50)")]
    [StringLength(50)]
    [Display(Name = "Category")]
    public string? Category { get; set; }

    public ICollection<SpaceEquipment> SpaceEquipments { get; set; } = new List<SpaceEquipment>();
}
