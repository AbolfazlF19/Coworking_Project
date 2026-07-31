using CoworkingSpace.Web.Data;
using CoworkingSpace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoworkingSpace.Web.Controllers;

[Authorize(Roles = "Admin")]
public class EquipmentController : Controller
{
    private readonly ApplicationDbContext _context;

    public EquipmentController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Equipment
    public async Task<IActionResult> Index()
    {
        var equipment = await _context.Equipment
            .Include(e => e.SpaceEquipments)
                .ThenInclude(se => se.Space)
            .OrderBy(e => e.Name)
            .ToListAsync();
        return View(equipment);
    }

    // GET: Equipment/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var equipment = await _context.Equipment
            .Include(e => e.SpaceEquipments)
                .ThenInclude(se => se.Space)
            .FirstOrDefaultAsync(e => e.EquipmentId == id);

        if (equipment == null) return NotFound();
        return View(equipment);
    }

    // GET: Equipment/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Equipment/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Category,Description")] Equipment equipment)
    {
        if (ModelState.IsValid)
        {
            _context.Add(equipment);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Equipment created successfully.";
            return RedirectToAction(nameof(Index));
        }
        return View(equipment);
    }

    // GET: Equipment/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var equipment = await _context.Equipment.FindAsync(id);
        if (equipment == null) return NotFound();
        return View(equipment);
    }

    // POST: Equipment/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("EquipmentId,Name,Category,Description")] Equipment equipment)
    {
        if (id != equipment.EquipmentId) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(equipment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Equipment updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EquipmentExists(equipment.EquipmentId)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(equipment);
    }

    // GET: Equipment/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var equipment = await _context.Equipment
            .Include(e => e.SpaceEquipments)
            .FirstOrDefaultAsync(e => e.EquipmentId == id);

        if (equipment == null) return NotFound();

        // بررسی وابستگی‌ها
        var hasRelatedSpaces = equipment.SpaceEquipments != null && equipment.SpaceEquipments.Any();

        ViewBag.HasRelatedSpaces = hasRelatedSpaces;
        ViewBag.RelatedSpacesCount = hasRelatedSpaces ? equipment.SpaceEquipments.Count() : 0;

        if (hasRelatedSpaces)
        {
            ViewBag.ErrorMessage = $"This equipment is currently assigned to {equipment.SpaceEquipments.Count()} space(s). Please remove these assignments first before deleting.";
        }
        else
        {
            ViewBag.ErrorMessage = null;
        }

        return View(equipment);
    }

    // POST: Equipment/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var equipment = await _context.Equipment
            .Include(e => e.SpaceEquipments)
            .FirstOrDefaultAsync(e => e.EquipmentId == id);

        if (equipment == null) return NotFound();

        // اگر وابستگی وجود دارد، اجازه حذف نمی‌دهیم
        if (equipment.SpaceEquipments != null && equipment.SpaceEquipments.Any())
        {
            TempData["Error"] = $"Cannot delete this equipment because it is assigned to {equipment.SpaceEquipments.Count()} space(s). Please remove the assignments first.";
            return RedirectToAction(nameof(Delete), new { id = equipment.EquipmentId });
        }

        _context.Equipment.Remove(equipment);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Equipment deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private bool EquipmentExists(int id) => _context.Equipment.Any(e => e.EquipmentId == id);
}