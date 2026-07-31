using System.ComponentModel.DataAnnotations;

namespace CoworkingSpace.Web.Models;

/// <summary>
/// A booking of a space by a member between two points in time.
/// The applicable hourly price and the resulting total are snapshotted
/// at creation time so historical figures are preserved.
/// Maps to table <c>Reservation</c>.
/// </summary>
public class Reservation
{
    [Column("reservation_id")]
    public int ReservationId { get; set; }

    [Column("member_id")]
    [Display(Name = "Member")]
    public int MemberId { get; set; }

    [Column("space_id")]
    [Display(Name = "Space")]
    public int SpaceId { get; set; }

    [Column("start_time")]
    [Display(Name = "Start Time")]
    public DateTime StartTime { get; set; }

    [Column("end_time")]
    [Display(Name = "End Time")]
    public DateTime EndTime { get; set; }

    [Column("reservation_status", TypeName = "varchar(50)")]
    [Display(Name = "Status")]
    public ReservationStatus ReservationStatus { get; set; } = ReservationStatus.Pending;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("notes", TypeName = "nvarchar(500)")]
    [DataType(DataType.MultilineText)]
    [StringLength(500)]
    public string? Notes { get; set; }

    [Column("applied_price_per_hour", TypeName = "decimal(10,2)")]
    [Display(Name = "Applied Price / Hour")]
    public decimal AppliedPricePerHour { get; set; }

    [Column("total_amount", TypeName = "decimal(10,2)")]
    [Display(Name = "Total Amount")]
    public decimal TotalAmount { get; set; }

    [ForeignKey(nameof(MemberId))]
    public Member? Member { get; set; }

    [ForeignKey(nameof(SpaceId))]
    public Space? Space { get; set; }

    /// <summary>One-to-one: a reservation has exactly one payment.</summary>
    public Payment? Payment { get; set; }
}
