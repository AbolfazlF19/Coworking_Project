using CoworkingSpace.Web.Data;
using CoworkingSpace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoworkingSpace.Web.Controllers;

[Authorize(Roles = "Admin")]
public class StaffController : Controller
{
    private readonly ApplicationDbContext _context;

    public StaffController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Staff
    public async Task<IActionResult> Index()
    {
        var staff = await _context.Staff
            .OrderBy(s => s.FullName)
            .ToListAsync();
        return View(staff);
    }

    // GET: Staff/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var staff = await _context.Staff
            .FirstOrDefaultAsync(s => s.StaffId == id);

        if (staff == null) return NotFound();
        return View(staff);
    }

    // GET: Staff/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Staff/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FullName,Role,Phone,Email")] Staff staff)
    {
        if (ModelState.IsValid)
        {
            _context.Add(staff);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Staff created successfully.";
            return RedirectToAction(nameof(Index));
        }
        return View(staff);
    }

    // GET: Staff/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var staff = await _context.Staff.FindAsync(id);
        if (staff == null) return NotFound();
        return View(staff);
    }

    // POST: Staff/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("StaffId,FullName,Role,Phone,Email")] Staff staff)
    {
        if (id != staff.StaffId) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(staff);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Staff updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StaffExists(staff.StaffId)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(staff);
    }

    // GET: Staff/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var staff = await _context.Staff
            .Include(s => s.Maintenances) // ← این خط را اضافه کن
            .FirstOrDefaultAsync(s => s.StaffId == id);

        if (staff == null) return NotFound();

        // بررسی وابستگی‌ها
        var hasRelatedMaintenances = staff.Maintenances != null && staff.Maintenances.Any();

        ViewBag.HasRelatedMaintenances = hasRelatedMaintenances;
        ViewBag.RelatedMaintenancesCount = hasRelatedMaintenances ? staff.Maintenances.Count() : 0;

        if (hasRelatedMaintenances)
        {
            ViewBag.ErrorMessage = $"This staff member is assigned to {staff.Maintenances.Count()} maintenance record(s). Please reassign or delete the maintenance records first before deleting this staff member.";
        }
        else
        {
            ViewBag.ErrorMessage = null;
        }

        return View(staff);
    }

    // POST: Staff/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var staff = await _context.Staff
            .Include(s => s.Maintenances) // ← این خط را اضافه کن
            .FirstOrDefaultAsync(s => s.StaffId == id);

        if (staff == null) return NotFound();

        // اگر وابستگی وجود دارد، اجازه حذف نمی‌دهیم
        if (staff.Maintenances != null && staff.Maintenances.Any())
        {
            TempData["Error"] = $"Cannot delete this staff member because they are assigned to {staff.Maintenances.Count()} maintenance record(s). Please reassign or delete the maintenance records first.";
            return RedirectToAction(nameof(Delete), new { id = staff.StaffId });
        }

        _context.Staff.Remove(staff);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Staff deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private bool StaffExists(int id) => _context.Staff.Any(e => e.StaffId == id);
}