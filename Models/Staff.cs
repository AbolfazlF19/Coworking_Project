using System.ComponentModel.DataAnnotations;

namespace CoworkingSpace.Web.Models;

/// <summary>
/// A building employee (Manager, Receptionist, Cleaner, Security).
/// Staff are NOT Identity users; they exist only as internal records
/// used for maintenance tracking.
/// Maps to table <c>Staff</c>.
/// </summary>
public class Staff
{
    [Column("staff_id")]
    public int StaffId { get; set; }

    [Column("full_name", TypeName = "nvarchar(100)")]
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [StringLength(50)]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    [Column("phone", TypeName = "nvarchar(20)")]
    [Required(ErrorMessage = "Phone is required.")]
    [StringLength(20)]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Column("email", TypeName = "varchar(50)")]
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [StringLength(50)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    public ICollection<SpaceMaintenance> Maintenances { get; set; } = new List<SpaceMaintenance>();
}
