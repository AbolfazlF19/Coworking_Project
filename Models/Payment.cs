using System.ComponentModel.DataAnnotations;

namespace CoworkingSpace.Web.Models;

/// <summary>
/// A payment for a reservation. The reservation_id is unique, enforcing a
/// strict one-to-one relationship with <see cref="Reservation"/>.
/// Maps to table <c>Payment</c>.
/// </summary>
public class Payment
{
    [Column("payment_id")]
    public int PaymentId { get; set; }

    [Column("price_id")]
    [Display(Name = "Price")]
    public int PriceId { get; set; }

    [Column("reservation_id")]
    [Display(Name = "Reservation")]
    public int ReservationId { get; set; }

    [Column("amount", TypeName = "decimal(10,2)")]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    [Column("payment_date")]
    [Display(Name = "Payment Date")]
    public DateTime PaymentDate { get; set; }

    [Column("payment_method", TypeName = "varchar(30)")]
    [Display(Name = "Method")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    [Column("payment_status", TypeName = "varchar(20)")]
    [Display(Name = "Status")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    [ForeignKey(nameof(PriceId))]
    public Price? Price { get; set; }

    [ForeignKey(nameof(ReservationId))]
    public Reservation? Reservation { get; set; }
}
