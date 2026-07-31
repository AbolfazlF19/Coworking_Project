using System.ComponentModel.DataAnnotations;

namespace CoworkingSpace.Web.Models;

/// <summary>
/// Join entity that assigns an equipment to a space with a quantity and notes.
/// Composite primary key (space_id, equipment_id). Maps to <c>SpaceEquipment</c>.
/// </summary>
public class SpaceEquipment
{
    [Column("space_id")]
    public int SpaceId { get; set; }

    [Column("equipment_id")]
    public int EquipmentId { get; set; }

    [Column("quantity", TypeName = "smallint")]
    [Range(1, short.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public short Quantity { get; set; } = 1;

    [Column("notes", TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(SpaceId))]
    public Space? Space { get; set; }

    [ForeignKey(nameof(EquipmentId))]
    public Equipment? Equipment { get; set; }
}
