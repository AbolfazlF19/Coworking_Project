using System.ComponentModel.DataAnnotations;

namespace CoworkingSpace.Web.Models;

/// <summary>
/// A co-working member. Each member may (optionally) be linked to an
/// Identity account through <see cref="UserId"/> (FK to AspNetUsers.Id).
/// Maps to table <c>Member</c>.
/// </summary>
public class Member
{
    [Column("member_id")]
    public int MemberId { get; set; }

    [Column("full_name", TypeName = "nvarchar(100)")]
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

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

    [Column("register_date", TypeName = "date")]
    [Display(Name = "Register Date")]
    public DateTime RegisterDate { get; set; }

    /// <summary>
    /// Foreign key to AspNetUsers.Id (nvarchar(450), nullable).
    /// </summary>
    [Column("UserId", TypeName = "nvarchar(450)")]
    [Display(Name = "Identity User")]
    public string? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
