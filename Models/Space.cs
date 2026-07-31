using System.ComponentModel.DataAnnotations;

namespace CoworkingSpace.Web.Models;

/// <summary>
/// A rentable room/space inside the co-working facility.
/// Maps to table <c>Spaces</c>.
/// </summary>
public class Space
{
    [Column("space_id")]
    public int SpaceId { get; set; }

    [Column("space_name", TypeName = "nvarchar(50)")]
    [Required(ErrorMessage = "Space name is required.")]
    [StringLength(50)]
    [Display(Name = "Space Name")]
    public string SpaceName { get; set; } = string.Empty;

    [Column("space_type", TypeName = "varchar(50)")]
    [Required(ErrorMessage = "Space type is required.")]
    [Display(Name = "Space Type")]
    public SpaceType SpaceType { get; set; }

    [Column("capacity")]
    [Required(ErrorMessage = "Capacity is required.")]
    [Range(1, 10000, ErrorMessage = "Capacity must be at least 1.")]
    public int Capacity { get; set; }

    [Column("is_active")]
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Column("location", TypeName = "nvarchar(200)")]
    [Required(ErrorMessage = "Location is required.")]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Price> Prices { get; set; } = new List<Price>();
    public virtual ICollection<SpaceImage> Images { get; set; } = new List<SpaceImage>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<SpaceEquipment> SpaceEquipments { get; set; } = new List<SpaceEquipment>();
    public ICollection<SpaceMaintenance> Maintenances { get; set; } = new List<SpaceMaintenance>();
}
