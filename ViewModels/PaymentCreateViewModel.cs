using System.ComponentModel.DataAnnotations;
using CoworkingSpace.Web.Models;

namespace CoworkingSpace.Web.ViewModels;

/// <summary>
/// Form model for recording a payment against a reservation.
/// </summary>
public class PaymentCreateViewModel
{
    public int ReservationId { get; set; }

    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Payment method is required.")]
    [Display(Name = "Payment Method")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    [Display(Name = "Status")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Paid;

    public int? SelectedPriceId { get; set; }
}
