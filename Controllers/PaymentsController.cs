using CoworkingSpace.Web.Data;
using CoworkingSpace.Web.Models;
using CoworkingSpace.Web.Services;
using CoworkingSpace.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoworkingSpace.Web.Controllers;

/// <summary>
/// Records payments against reservations. Each reservation has exactly one
/// payment (reservation_id is unique). The amount is always the reservation's
/// total_amount; only method/status can be chosen.
/// </summary>
[Authorize]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PricingService _pricing;

    public PaymentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        PricingService pricing)
    {
        _context = context;
        _userManager = userManager;
        _pricing = pricing;
    }

    // GET: Payments
    public async Task<IActionResult> Index()
    {
        var query = _context.Payments
            .Include(p => p.Reservation).ThenInclude(r => r!.Member)
            .Include(p => p.Reservation).ThenInclude(r => r!.Space)
            .Include(p => p.Price)
            .AsQueryable();

        if (User.IsInRole("Member"))
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return Forbid();
            query = query.Where(p => p.Reservation!.MemberId == member.MemberId);
        }

        var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
        return View(payments);
    }

    // GET: Payments/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var payment = await _context.Payments
            .Include(p => p.Reservation).ThenInclude(r => r!.Member)
            .Include(p => p.Reservation).ThenInclude(r => r!.Space)
            .Include(p => p.Price)
            .FirstOrDefaultAsync(p => p.PaymentId == id);

        if (payment == null) return NotFound();

        if (!await CanAccessAsync(payment.Reservation!.MemberId))
        {
            return Forbid();
        }

        return View(payment);
    }

    // GET: Payments/Create?reservationId=5
    public async Task<IActionResult> Create(int reservationId)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Member)
            .Include(r => r.Space)
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

        if (reservation == null) return NotFound();

        if (!await CanAccessAsync(reservation.MemberId))
        {
            return Forbid();
        }

        // Enforce one-to-one.
        if (await _context.Payments.AnyAsync(p => p.ReservationId == reservationId))
        {
            TempData["Error"] = "A payment already exists for this reservation.";
            return RedirectToAction(nameof(ReservationsController.Details), "Reservations", new { id = reservationId });
        }

        var model = new PaymentCreateViewModel
        {
            ReservationId = reservationId,
            Amount = reservation.TotalAmount
        };

        ViewBag.Reservation = reservation;
        return View(model);
    }

    // POST: Payments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentCreateViewModel model)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Space)
            .FirstOrDefaultAsync(r => r.ReservationId == model.ReservationId);

        if (reservation == null) return NotFound();

        if (!await CanAccessAsync(reservation.MemberId))
        {
            return Forbid();
        }

        if (await _context.Payments.AnyAsync(p => p.ReservationId == model.ReservationId))
        {
            ModelState.AddModelError(string.Empty, "A payment already exists for this reservation.");
        }

        // The payment amount must equal the reservation's total.
        if (model.Amount != reservation.TotalAmount)
        {
            model.Amount = reservation.TotalAmount; // lock to total
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Reservation = reservation;
            return View(model);
        }

        // Resolve the price to satisfy the required price_id FK.
        var price = await _pricing.ResolvePriceAsync(reservation.SpaceId, reservation.StartTime, reservation.EndTime)
                    ?? await _context.Prices.Where(p => p.SpaceId == reservation.SpaceId)
                        .OrderByDescending(p => p.EffectiveFrom).FirstOrDefaultAsync();

        if (price == null)
        {
            ModelState.AddModelError(string.Empty, "No price record found for this reservation's space.");
            ViewBag.Reservation = reservation;
            return View(model);
        }

        var payment = new Payment
        {
            ReservationId = reservation.ReservationId,
            PriceId = price.PriceId,
            Amount = reservation.TotalAmount,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = model.PaymentMethod,
            PaymentStatus = model.PaymentStatus
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Payment recorded ({payment.Amount:C}, {payment.PaymentStatus}).";
        return RedirectToAction(nameof(Details), new { id = payment.PaymentId });
    }

    // GET: Payments/Edit/5  (Admin, Staff)
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var payment = await _context.Payments
            .Include(p => p.Reservation)
            .FirstOrDefaultAsync(p => p.PaymentId == id);

        if (payment == null) return NotFound();

        ViewBag.Reservation = payment.Reservation;
        return View(payment);
    }

    // POST: Payments/Edit/5  (Admin, Staff)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Edit(int id, [Bind("PaymentId,PriceId,ReservationId,Amount,PaymentDate,PaymentMethod,PaymentStatus")] Payment payment)
    {
        if (id != payment.PaymentId) return NotFound();

        // Lock amount to the reservation total.
        var reservation = await _context.Reservations.FindAsync(payment.ReservationId);
        if (reservation != null)
        {
            payment.Amount = reservation.TotalAmount;
        }

        ModelState.Remove("Reservation");
        ModelState.Remove("Price");

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(payment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Payment updated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Payments.AnyAsync(p => p.PaymentId == payment.PaymentId)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Reservation = reservation;
        return View(payment);
    }

    // GET: Payments/Delete/5  (Admin only)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var payment = await _context.Payments
            .Include(p => p.Reservation).ThenInclude(r => r!.Member)
            .Include(p => p.Reservation).ThenInclude(r => r!.Space)
            .FirstOrDefaultAsync(p => p.PaymentId == id);

        if (payment == null) return NotFound();
        return View(payment);
    }

    // POST: Payments/Delete/5  (Admin only)
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment != null)
        {
            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Payment deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ----------------- helpers -----------------

    private async Task<bool> CanAccessAsync(int memberId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Staff")) return true;

        var member = await GetCurrentMemberAsync();
        return member != null && member.MemberId == memberId;
    }

    private async Task<Member?> GetCurrentMemberAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return null;
        return await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
    }
}
