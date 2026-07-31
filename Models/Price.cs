using System.ComponentModel.DataAnnotations;

namespace CoworkingSpace.Web.Models;

/// <summary>
/// Time-boxed price-per-hour for a space. A space may have many price records
/// over time; the correct one is resolved when a reservation is created.
/// Maps to table <c>Price</c>.
/// </summary>
public class Price
{
    [Column("price_id")]
    public int PriceId { get; set; }

    [Column("space_id")]
    [Display(Name = "Space")]
    public int SpaceId { get; set; }

    [Column("price_per_hour", TypeName = "decimal(10,2)")]
    [Display(Name = "Price per Hour")]
    [Range(0.01, 100000, ErrorMessage = "Price must be greater than 0.")]
    public decimal PricePerHour { get; set; }

    [Column("effective_from")]
    [Display(Name = "Effective From")]
    public DateTime EffectiveFrom { get; set; }

    [Column("effective_to")]
    [Display(Name = "Effective To")]
    public DateTime EffectiveTo { get; set; }

    [ForeignKey(nameof(SpaceId))]
    public Space? Space { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
