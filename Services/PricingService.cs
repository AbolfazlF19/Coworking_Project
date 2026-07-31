using CoworkingSpace.Web.Data;
using CoworkingSpace.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CoworkingSpace.Web.Services;

/// <summary>
/// Resolves the applicable <see cref="Price"/> for a booking window.
/// Rule (from spec):
///   1. Prefer a price whose [effective_from, effective_to] fully covers the
///      reservation window (effective_from <= start AND effective_to >= end).
///   2. Otherwise fall back to a price that at least covers the start time.
///   3. If several match, pick the one with the latest effective_from.
/// </summary>
public class PricingService
{
    private readonly ApplicationDbContext _db;

    public PricingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Price?> ResolvePriceAsync(int spaceId, DateTime start, DateTime end)
    {
        var candidates = await _db.Prices
            .Where(p => p.SpaceId == spaceId)
            .AsNoTracking()
            .ToListAsync();

        if (candidates.Count == 0) return null;

        // 1. Full coverage of the booking window.
        var price = candidates
            .Where(p => p.EffectiveFrom <= start && p.EffectiveTo >= end)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefault();

        // 2. Fallback: covers at least the start time.
        price ??= candidates
            .Where(p => p.EffectiveFrom <= start && p.EffectiveTo >= start)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefault();

        return price;
    }

    /// <summary>
    /// Computes the total amount for a booking window against an hourly price.
    /// </summary>
    public static decimal CalculateTotal(DateTime start, DateTime end, decimal pricePerHour)
    {
        var hours = (decimal)(end - start).TotalHours;
        return Math.Round(hours * pricePerHour, 2, MidpointRounding.AwayFromZero);
    }
}
