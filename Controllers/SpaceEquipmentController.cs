using CoworkingSpace.Web.Data;
using CoworkingSpace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoworkingSpace.Web.Controllers;

[Authorize(Roles = "Admin")]
public class SpaceEquipmentController : Controller
{
    private readonly ApplicationDbContext _context;

    public SpaceEquipmentController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: SpaceEquipment
    public async Task<IActionResult> Index()
    {
        var list = await _context.SpaceEquipment
            .Include(se => se.Space)
            .Include(se => se.Equipment)
            .OrderBy(se => se.Space!.SpaceName)
            .ThenBy(se => se.Equipment!.Name)
            .ToListAsync();
        return View(list);
    }

    // GET: SpaceEquipment/Create
    public IActionResult Create()
    {
        PopulateDropdowns();
        return View();
    }

    // POST: SpaceEquipment/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SpaceId,EquipmentId,Quantity,Notes")] SpaceEquipment spaceEquipment)
    {
        // Don't validate nav properties.
        ModelState.Remove("Space");
        ModelState.Remove("Equipment");

        bool exists = await _context.SpaceEquipment
            .AnyAsync(se => se.SpaceId == spaceEquipment.SpaceId
                         && se.EquipmentId == spaceEquipment.EquipmentId);

        if (exists)
        {
            ModelState.AddModelError(string.Empty, "This equipment is already assigned to this space.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(spaceEquipment);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Equipment assigned to space.";
            return RedirectToAction(nameof(Index));
        }

        PopulateDropdowns(spaceEquipment.SpaceId, spaceEquipment.EquipmentId);
        return View(spaceEquipment);
    }

    // GET: SpaceEquipment/Edit?spaceId=1&equipmentId=2
    public async Task<IActionResult> Edit(int spaceId, int equipmentId)
    {
        var spaceEquipment = await _context.SpaceEquipment
            .Include(se => se.Space)
            .Include(se => se.Equipment)
            .FirstOrDefaultAsync(se => se.SpaceId == spaceId && se.EquipmentId == equipmentId);

        if (spaceEquipment == null) return NotFound();
        return View(spaceEquipment);
    }

    // POST: SpaceEquipment/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int spaceId, int equipmentId, [Bind("SpaceId,EquipmentId,Quantity,Notes")] SpaceEquipment spaceEquipment)
    {
        if (spaceId != spaceEquipment.SpaceId || equipmentId != spaceEquipment.EquipmentId)
            return NotFound();

        ModelState.Remove("Space");
        ModelState.Remove("Equipment");

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(spaceEquipment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Assignment updated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.SpaceEquipment.AnyAsync(se => se.SpaceId == spaceId && se.EquipmentId == equipmentId))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(spaceEquipment);
    }

    // GET: SpaceEquipment/Delete
    public async Task<IActionResult> Delete(int spaceId, int equipmentId)
    {
        var spaceEquipment = await _context.SpaceEquipment
            .Include(se => se.Space)
            .Include(se => se.Equipment)
            .FirstOrDefaultAsync(se => se.SpaceId == spaceId && se.EquipmentId == equipmentId);

        if (spaceEquipment == null) return NotFound();
        return View(spaceEquipment);
    }

    // POST: SpaceEquipment/Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int spaceId, int equipmentId)
    {
        var spaceEquipment = await _context.SpaceEquipment
            .FirstOrDefaultAsync(se => se.SpaceId == spaceId && se.EquipmentId == equipmentId);

        if (spaceEquipment != null)
        {
            _context.SpaceEquipment.Remove(spaceEquipment);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Assignment removed.";
        }
        return RedirectToAction(nameof(Index));
    }

    private void PopulateDropdowns(object? selectedSpace = null, object? selectedEquipment = null)
    {
        ViewBag.SpaceId = new SelectList(_context.Spaces.OrderBy(s => s.SpaceName), "SpaceId", "SpaceName", selectedSpace);
        ViewBag.EquipmentId = new SelectList(_context.Equipment.OrderBy(e => e.Name), "EquipmentId", "Name", selectedEquipment);
    }
}
