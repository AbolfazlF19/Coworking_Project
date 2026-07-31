using System.ComponentModel.DataAnnotations;

namespace CoworkingSpace.Web.Models;

/// <summary>
/// A maintenance record tracked against a space and handled by a staff member.
/// When <see cref="AffectsReservation"/> is true and the date range overlaps
/// active reservations, those reservations are automatically cancelled.
/// Maps to table <c>SpaceMaintenance</c>.
/// </summary>
public class SpaceMaintenance
{
    [Key]
    [Column("maintenance_id")]
    public int MaintenanceId { get; set; }

    [Column("space_id")]
    [Display(Name = "Space")]
    public int SpaceId { get; set; }

    [Column("staff_id")]
    [Display(Name = "Staff")]
    public int StaffId { get; set; }

    [Column("start_date")]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; }

    [Column("description", TypeName = "nvarchar(200)")]
    [Required(ErrorMessage = "Description is required.")]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [Column("maintenance_type", TypeName = "nvarchar(50)")]
    [Display(Name = "Type")]
    public MaintenanceType MaintenanceType { get; set; } = MaintenanceType.Cleaning;

    [Column("affects_reservation")]
    [Display(Name = "Affects Reservations")]
    public bool AffectsReservation { get; set; }

    [Column("status", TypeName = "nvarchar(20)")]
    [Display(Name = "Status")]
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

    [ForeignKey(nameof(SpaceId))]
    public Space? Space { get; set; }

    [ForeignKey(nameof(StaffId))]
    public Staff? Staff { get; set; }
}
