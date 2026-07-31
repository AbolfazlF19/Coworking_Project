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

[Authorize]
public class ReservationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PricingService _pricing;

    private const int BufferMinutes = 30; // بافر تمیزکاری

    public ReservationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        PricingService pricing)
    {
        _context = context;
        _userManager = userManager;
        _pricing = pricing;
    }

    // ---------------------------- Index ----------------------------
    public async Task<IActionResult> Index()
    {
        var query = _context.Reservations
            .Include(r => r.Member)
            .Include(r => r.Space)
            .ThenInclude(s => s.Images)
            .Include(r => r.Payment)
            .AsQueryable();

        if (User.IsInRole("Member"))
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return Forbid();
            query = query.Where(r => r.MemberId == member.MemberId);
        }

        var reservations = await query.OrderByDescending(r => r.StartTime).ToListAsync();
        return View(reservations);
    }

    // ---------------------------- Details ----------------------------
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var reservation = await _context.Reservations
            .Include(r => r.Member).ThenInclude(m => m!.User)
            .Include(r => r.Space)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.ReservationId == id);

        if (reservation == null) return NotFound();
        if (!await CanAccessAsync(reservation.MemberId)) return Forbid();

        return View(reservation);
    }

    // ---------------------------- Create (GET) ----------------------------
    public IActionResult Create(int? spaceId)
    {
        var spaces = _context.Spaces
            .Where(s => s.IsActive)
            .Include(s => s.Images.OrderBy(i => i.DisplayOrder))
            .ToList();

        var model = new ReservationCreateViewModel
        {
            SpaceId = spaceId ?? 0,
            StartTime = DateTime.Now.AddHours(1).AddSeconds(-DateTime.Now.Second).AddMilliseconds(-DateTime.Now.Millisecond),
            EndTime = DateTime.Now.AddHours(2).AddSeconds(-DateTime.Now.Second).AddMilliseconds(-DateTime.Now.Millisecond),
            AvailableSpaces = spaces
        };

        if (!User.IsInRole("Member"))
        {
            ViewBag.Members = new SelectList(
                _context.Members.OrderBy(m => m.FullName),
                "MemberId", "FullName");
        }

        return View(model);
    }

    // ---------------------------- Create (POST) ----------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationCreateViewModel model, int? memberOverride)
    {
        PopulateViewBags();

        // ===== اعتبارسنجی تاریخ‌ها =====
        if (model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError("EndTime", "End time must be after start time.");
        }

        // ===== بررسی ساعات غیرفعال (۲۳:۰۰ تا ۰۸:۰۰) =====
        if (IsWithinClosedHours(model.StartTime, model.EndTime))
        {
            ModelState.AddModelError(string.Empty,
                "Reservations are not allowed between 23:00 and 08:00. Please choose another time.");
        }

        var space = await _context.Spaces.FindAsync(model.SpaceId);
        if (space == null)
        {
            ModelState.AddModelError("SpaceId", "Please select a valid space.");
        }
        else if (!space.IsActive)
        {
            ModelState.AddModelError("SpaceId", "This space is not currently available.");
        }

        if (!ModelState.IsValid) return View(model);

        // ===== محدودیت شروع رزرو برای ممبرها (حداقل ۲ ساعت آینده) =====
        if (User.IsInRole("Member"))
        {
            var minStartTime = DateTime.Now.AddHours(2);
            if (model.StartTime < minStartTime)
            {
                ModelState.AddModelError("StartTime", "Reservations must be made at least 2 hours in advance.");
                return View(model);
            }
        }

        // ===== تعیین MemberId =====
        int memberId;
        if (User.IsInRole("Member"))
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                ModelState.AddModelError(string.Empty, "Your account is not linked to a member record.");
                return View(model);
            }
            memberId = member.MemberId;
        }
        else
        {
            memberId = memberOverride ?? 0;
            if (!await _context.Members.AnyAsync(m => m.MemberId == memberId))
            {
                ModelState.AddModelError(string.Empty, "Please select a valid member.");
                return View(model);
            }
        }

        // ===== محاسبه قیمت =====
        var price = await _pricing.ResolvePriceAsync(model.SpaceId, model.StartTime, model.EndTime);
        if (price == null)
        {
            ModelState.AddModelError(string.Empty,
                "No applicable price found for the selected space and time range.");
            return View(model);
        }

        // ===== بررسی تداخل با بافر ۳۰ دقیقه‌ای =====
        var bufferStart = model.StartTime.AddMinutes(-BufferMinutes);
        var bufferEnd = model.EndTime.AddMinutes(BufferMinutes);

        var overlap = await _context.Reservations.AnyAsync(r =>
            r.SpaceId == model.SpaceId &&
            r.StartTime < bufferEnd &&
            r.EndTime > bufferStart &&
            (r.ReservationStatus == ReservationStatus.Pending ||
             r.ReservationStatus == ReservationStatus.Confirmed));
        if (overlap)
        {
            ModelState.AddModelError(string.Empty,
                "This space is already booked (including 30-minute cleaning buffer). Please choose another time.");
            return View(model);
        }

        // ===== ذخیره رزرو =====
        var total = PricingService.CalculateTotal(model.StartTime, model.EndTime, price.PricePerHour);

        var reservation = new Reservation
        {
            MemberId = memberId,
            SpaceId = model.SpaceId,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            ReservationStatus = ReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Notes = model.Notes,
            AppliedPricePerHour = price.PricePerHour,
            TotalAmount = total
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Reservation created. Total: {total:C} (@ {price.PricePerHour:C}/hr).";
        return RedirectToAction(nameof(Details), new { id = reservation.ReservationId });
    }

    // ---------------------------- Edit (GET) ----------------------------
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var reservation = await _context.Reservations
            .Include(r => r.Space)
            .Include(r => r.Member)
            .FirstOrDefaultAsync(r => r.ReservationId == id);

        if (reservation == null) return NotFound();

        reservation.StartTime = reservation.StartTime.AddSeconds(-reservation.StartTime.Second).AddMilliseconds(-reservation.StartTime.Millisecond);
        reservation.EndTime = reservation.EndTime.AddSeconds(-reservation.EndTime.Second).AddMilliseconds(-reservation.EndTime.Millisecond);

        ViewBag.SpaceId = new SelectList(
            _context.Spaces.Where(s => s.IsActive).OrderBy(s => s.SpaceName),
            "SpaceId", "SpaceName", reservation.SpaceId);

        return View(reservation);
    }

    // ---------------------------- Edit (POST) ----------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, Reservation model)
    {
        if (id != model.ReservationId) return NotFound();

        var reservation = await _context.Reservations
            .Include(r => r.Space)
            .FirstOrDefaultAsync(r => r.ReservationId == id);

        if (reservation == null) return NotFound();

        // ===== اعتبارسنجی =====
        if (model.StartTime >= model.EndTime)
        {
            ModelState.AddModelError("EndTime", "End time must be after start time.");
        }

        if (IsWithinClosedHours(model.StartTime, model.EndTime))
        {
            ModelState.AddModelError(string.Empty,
                "Reservations are not allowed between 23:00 and 08:00.");
        }

        var space = await _context.Spaces.FindAsync(model.SpaceId);
        if (space == null || !space.IsActive)
        {
            ModelState.AddModelError("SpaceId", "Selected space is not available.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.SpaceId = new SelectList(
                _context.Spaces.Where(s => s.IsActive).OrderBy(s => s.SpaceName),
                "SpaceId", "SpaceName", model.SpaceId);
            return View(model);
        }

        // ===== بررسی تداخل با بافر =====
        var bufferStart = model.StartTime.AddMinutes(-BufferMinutes);
        var bufferEnd = model.EndTime.AddMinutes(BufferMinutes);

        var overlap = await _context.Reservations.AnyAsync(r =>
            r.SpaceId == model.SpaceId &&
            r.ReservationId != id &&
            r.StartTime < bufferEnd &&
            r.EndTime > bufferStart &&
            (r.ReservationStatus == ReservationStatus.Pending ||
             r.ReservationStatus == ReservationStatus.Confirmed));
        if (overlap)
        {
            ModelState.AddModelError(string.Empty,
                "This space is already booked (including 30-minute cleaning buffer).");
            ViewBag.SpaceId = new SelectList(
                _context.Spaces.Where(s => s.IsActive).OrderBy(s => s.SpaceName),
                "SpaceId", "SpaceName", model.SpaceId);
            return View(model);
        }

        // ===== محاسبه قیمت و ذخیره =====
        var price = await _pricing.ResolvePriceAsync(model.SpaceId, model.StartTime, model.EndTime);
        if (price == null)
        {
            ModelState.AddModelError(string.Empty,
                "No applicable price found for the selected space and time range.");
            ViewBag.SpaceId = new SelectList(
                _context.Spaces.Where(s => s.IsActive).OrderBy(s => s.SpaceName),
                "SpaceId", "SpaceName", model.SpaceId);
            return View(model);
        }

        reservation.SpaceId = model.SpaceId;
        reservation.StartTime = model.StartTime;
        reservation.EndTime = model.EndTime;
        reservation.Notes = model.Notes;
        reservation.AppliedPricePerHour = price.PricePerHour;
        reservation.TotalAmount = PricingService.CalculateTotal(model.StartTime, model.EndTime, price.PricePerHour);
        reservation.UpdatedAt = DateTime.UtcNow;

        _context.Update(reservation);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Reservation updated. New total: {reservation.TotalAmount:C}.";
        return RedirectToAction(nameof(Details), new { id = reservation.ReservationId });
    }

    // ---------------------------- GetBusySlots (AJAX) ----------------------------
    public async Task<IActionResult> GetBusySlots(int spaceId, DateTime start, DateTime end)
    {
        var bufferStart = start.AddMinutes(-BufferMinutes);
        var bufferEnd = end.AddMinutes(BufferMinutes);

        // 1. رزروهای فعال با بافر
        var reservations = await _context.Reservations
            .Where(r => r.SpaceId == spaceId &&
                        r.StartTime < bufferEnd &&
                        r.EndTime > bufferStart &&
                        (r.ReservationStatus == ReservationStatus.Pending ||
                         r.ReservationStatus == ReservationStatus.Confirmed))
            .Select(r => new
            {
                StartTime = r.StartTime.AddMinutes(-BufferMinutes),
                EndTime = r.EndTime.AddMinutes(BufferMinutes),
                Type = "Reservation",
                ReservationStatus = r.ReservationStatus.ToString(),
                MemberName = r.Member != null ? r.Member.FullName : "Unknown",
                OriginalStart = r.StartTime,
                OriginalEnd = r.EndTime
            })
            .ToListAsync();

        // 2. نگهداری‌های فعال با بافر
        var maintenances = await _context.SpaceMaintenances
            .Where(m => m.SpaceId == spaceId &&
                        m.StartDate < bufferEnd &&
                        m.EndDate > bufferStart &&
                        (m.Status == MaintenanceStatus.Scheduled ||
                         m.Status == MaintenanceStatus.InProgress))
            .Select(m => new
            {
                StartTime = m.StartDate.AddMinutes(-BufferMinutes),
                EndTime = m.EndDate.AddMinutes(BufferMinutes),
                Type = "Maintenance",
                ReservationStatus = (string?)null,
                MemberName = m.Description ?? "Maintenance",
                OriginalStart = m.StartDate,
                OriginalEnd = m.EndDate
            })
            .ToListAsync();

        // 3. ترکیب نتایج
        var busySlots = reservations.Concat(maintenances)
            .OrderBy(s => s.StartTime)
            .Select(s => new
            {
                s.StartTime,
                s.EndTime,
                s.Type,
                s.ReservationStatus,
                s.MemberName,
                s.OriginalStart,
                s.OriginalEnd
            });

        return Json(busySlots);
    }

    // ---------------------------- سایر اکشن‌ها (Confirm, Complete, Cancel, Delete) ----------------------------
    // (بدون تغییر - همان کد قبلی)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Confirm(int id)
    {
        return await ChangeStatusAsync(id, ReservationStatus.Confirmed, "Reservation confirmed.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Complete(int id)
    {
        return await ChangeStatusAsync(id, ReservationStatus.Completed, "Reservation marked completed.");
    }

    // POST: Reservations/Cancel/5  (Admin, Staff, or owning Member)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null) return NotFound();

        if (!await CanAccessAsync(reservation.MemberId))
        {
            return Forbid();
        }

        // ===== محدودیت ۲۴ ساعته برای ممبرها =====
        if (User.IsInRole("Member"))
        {
            var timeUntilStart = reservation.StartTime - DateTime.Now;
            if (timeUntilStart.TotalHours < 24)
            {
                TempData["Error"] = "You cannot cancel a reservation less than 24 hours before the start time.";
                return RedirectToAction(nameof(Details), new { id = reservation.ReservationId });
            }
        }

        reservation.ReservationStatus = ReservationStatus.Cancelled;
        reservation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Reservation cancelled.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var reservation = await _context.Reservations
            .Include(r => r.Member)
            .Include(r => r.Space)
            .FirstOrDefaultAsync(r => r.ReservationId == id);

        if (reservation == null) return NotFound();
        return View(reservation);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.ReservationId == id);

        if (reservation != null)
        {
            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Reservation deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------- Helper Methods ----------------------------
    private async Task<IActionResult> ChangeStatusAsync(int id, ReservationStatus status, string message)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null) return NotFound();

        reservation.ReservationStatus = status;
        reservation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

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

    private void PopulateViewBags()
    {
        var spaces = _context.Spaces.Where(s => s.IsActive).OrderBy(s => s.SpaceName).ToList();

        ViewBag.SpaceId = new SelectList(spaces, "SpaceId", "SpaceName");
        ViewBag.Spaces = new SelectList(spaces, "SpaceId", "SpaceName");

        if (!User.IsInRole("Member"))
        {
            ViewBag.Members = new SelectList(
                _context.Members.OrderBy(m => m.FullName),
                "MemberId", "FullName");
        }
    }

    // ===== بررسی ساعات غیرفعال (۲۳:۰۰ تا ۰۸:۰۰) =====
    private bool IsWithinClosedHours(DateTime start, DateTime end)
    {
        // اگر بازه بیشتر از ۲۴ ساعت باشد، حتماً با ساعات غیرفعال تداخل دارد
        if ((end - start).TotalHours > 24) return true;

        var current = start.Date;
        var endDate = end.Date;

        while (current <= endDate)
        {
            var closedStart = current.Date.AddHours(23); // 23:00
            var closedEnd = current.Date.AddDays(1).AddHours(8); // 08:00 روز بعد

            // اگر بازه با محدوده‌ی بسته تداخل داشته باشد
            if (start < closedEnd && end > closedStart)
                return true;

            current = current.AddDays(1);
        }
        return false;
    }
}