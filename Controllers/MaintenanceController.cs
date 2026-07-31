using CoworkingSpace.Web.Data;
using CoworkingSpace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoworkingSpace.Web.Controllers;

/// <summary>
/// Tracks maintenance records for spaces. When a record is created/updated
/// with <see cref="SpaceMaintenance.AffectsReservation"/> = true and its date
/// range overlaps active (Pending/Confirmed) reservations, those reservations
/// are automatically cancelled with an explanatory note.
/// </summary>
[Authorize(Roles = "Admin")]
public class MaintenanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(ApplicationDbContext context, ILogger<MaintenanceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Maintenance
    public async Task<IActionResult> Index()
    {
        var list = await _context.SpaceMaintenances
            .Include(m => m.Space)
            .Include(m => m.Staff)
            .OrderByDescending(m => m.StartDate)
            .ToListAsync();
        return View(list);
    }

    // GET: Maintenance/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var maintenance = await _context.SpaceMaintenances
            .Include(m => m.Space)
            .Include(m => m.Staff)
            .FirstOrDefaultAsync(m => m.MaintenanceId == id);

        if (maintenance == null) return NotFound();
        return View(maintenance);
    }

    // GET: Maintenance/Create
    public IActionResult Create()
    {
        PopulateDropdowns();
        return View();
    }

    // POST: Maintenance/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SpaceId,StaffId,StartDate,EndDate,Description,MaintenanceType,AffectsReservation,Status")] SpaceMaintenance maintenance)
    {
        ModelState.Remove("Space");
        ModelState.Remove("Staff");

        if (maintenance.EndDate < maintenance.StartDate)
        {
            ModelState.AddModelError("EndDate", "End Date must be on or after Start Date.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(maintenance);
            await _context.SaveChangesAsync();

            var cancelled = await ApplyMaintenanceEffectAsync(maintenance);
            TempData["Success"] = cancelled > 0
                ? $"Maintenance created. {cancelled} overlapping reservation(s) auto-cancelled."
                : "Maintenance created.";

            return RedirectToAction(nameof(Index));
        }

        PopulateDropdowns(maintenance.SpaceId, maintenance.StaffId);
        return View(maintenance);
    }

    // GET: Maintenance/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var maintenance = await _context.SpaceMaintenances.FindAsync(id);
        if (maintenance == null) return NotFound();

        PopulateDropdowns(maintenance.SpaceId, maintenance.StaffId);
        return View(maintenance);
    }

    // POST: Maintenance/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("MaintenanceId,SpaceId,StaffId,StartDate,EndDate,Description,MaintenanceType,AffectsReservation,Status")] SpaceMaintenance maintenance)
    {
        if (id != maintenance.MaintenanceId) return NotFound();
        ModelState.Remove("Space");
        ModelState.Remove("Staff");

        if (maintenance.EndDate < maintenance.StartDate)
        {
            ModelState.AddModelError("EndDate", "End Date must be on or after Start Date.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(maintenance);
                await _context.SaveChangesAsync();

                var cancelled = await ApplyMaintenanceEffectAsync(maintenance);
                TempData["Success"] = cancelled > 0
                    ? $"Maintenance updated. {cancelled} overlapping reservation(s) auto-cancelled."
                    : "Maintenance updated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.SpaceMaintenances.AnyAsync(m => m.MaintenanceId == maintenance.MaintenanceId))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        PopulateDropdowns(maintenance.SpaceId, maintenance.StaffId);
        return View(maintenance);
    }

    // GET: Maintenance/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var maintenance = await _context.SpaceMaintenances
            .Include(m => m.Space)
            .Include(m => m.Staff)
            .FirstOrDefaultAsync(m => m.MaintenanceId == id);
        if (maintenance == null) return NotFound();
        return View(maintenance);
    }

    // POST: Maintenance/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var maintenance = await _context.SpaceMaintenances.FindAsync(id);
        if (maintenance != null)
        {
            _context.SpaceMaintenances.Remove(maintenance);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Maintenance record deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Business rule: when maintenance affects reservations and overlaps
    /// active reservations, cancel them with an explanatory note.
    /// Returns the number of reservations cancelled.
    /// </summary>
    private async Task<int> ApplyMaintenanceEffectAsync(SpaceMaintenance maintenance)
    {
        if (!maintenance.AffectsReservation) return 0;

        var now = DateTime.UtcNow;

        var overlapping = await _context.Reservations
            .Where(r => r.SpaceId == maintenance.SpaceId
                     && (r.ReservationStatus == ReservationStatus.Pending
                         || r.ReservationStatus == ReservationStatus.Confirmed)
                     && r.StartTime < maintenance.EndDate
                     && r.EndTime > maintenance.StartDate)
            .ToListAsync();

        foreach (var r in overlapping)
        {
            r.ReservationStatus = ReservationStatus.Cancelled;
            r.UpdatedAt = now;
            var note = $"[Auto-cancelled by maintenance #{maintenance.MaintenanceId} ({maintenance.MaintenanceType}) on {now:yyyy-MM-dd HH:mm}]";
            r.Notes = string.IsNullOrWhiteSpace(r.Notes) ? note : $"{r.Notes}\n{note}";

            _logger.LogInformation(
                "Reservation {ReservationId} auto-cancelled due to maintenance {MaintenanceId} on space {SpaceId}.",
                r.ReservationId, maintenance.MaintenanceId, maintenance.SpaceId);
        }

        if (overlapping.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return overlapping.Count;
    }

    private void PopulateDropdowns(object? selectedSpace = null, object? selectedStaff = null)
    {
        ViewBag.SpaceId = new SelectList(_context.Spaces.OrderBy(s => s.SpaceName), "SpaceId", "SpaceName", selectedSpace);
        ViewBag.StaffId = new SelectList(_context.Staff.OrderBy(s => s.FullName), "StaffId", "FullName", selectedStaff);
    }
}
