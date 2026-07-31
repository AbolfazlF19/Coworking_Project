using CoworkingSpace.Web.Data;
using CoworkingSpace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoworkingSpace.Web.Controllers;

[Authorize(Roles = "Admin")]
public class PricesController : Controller
{
    private readonly ApplicationDbContext _context;

    public PricesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Prices
    public async Task<IActionResult> Index()
    {
        var prices = await _context.Prices
            .Include(p => p.Space)
            .OrderByDescending(p => p.EffectiveFrom)
            .ToListAsync();
        return View(prices);
    }

    // GET: Prices/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var price = await _context.Prices.Include(p => p.Space).FirstOrDefaultAsync(p => p.PriceId == id);
        if (price == null) return NotFound();
        return View(price);
    }

    // GET: Prices/Create
    public IActionResult Create()
    {
        ViewBag.SpaceId = new SelectList(_context.Spaces.OrderBy(s => s.SpaceName), "SpaceId", "SpaceName");
        return View();
    }

    // POST: Prices/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SpaceId,PricePerHour,EffectiveFrom,EffectiveTo")] Price price)
    {
        if (ModelState.IsValid)
        {
            // ===== بررسی تداخل زمانی =====
            var overlap = await _context.Prices.AnyAsync(p =>
                p.SpaceId == price.SpaceId &&
                p.EffectiveFrom < price.EffectiveTo &&
                p.EffectiveTo > price.EffectiveFrom);

            if (overlap)
            {
                ModelState.AddModelError(string.Empty,
                    "A price already exists for this space in the selected time range. Please choose a non-overlapping period.");
                PopulateViewBags();
                return View(price);
            }

            _context.Add(price);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Price created successfully.";
            return RedirectToAction(nameof(Index));
        }
        PopulateViewBags();
        return View(price);
    }

    // GET: Prices/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var price = await _context.Prices.FindAsync(id);
        if (price == null) return NotFound();

        ViewBag.SpaceId = new SelectList(_context.Spaces, "SpaceId", "SpaceName", price.SpaceId);
        return View(price);
    }

    // POST: Prices/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("PriceId,SpaceId,PricePerHour,EffectiveFrom,EffectiveTo")] Price price)
    {
        if (id != price.PriceId) return NotFound();

        if (ModelState.IsValid)
        {
            // ===== بررسی تداخل زمانی (به جز خود این قیمت) =====
            var overlap = await _context.Prices.AnyAsync(p =>
                p.SpaceId == price.SpaceId &&
                p.PriceId != price.PriceId &&
                p.EffectiveFrom < price.EffectiveTo &&
                p.EffectiveTo > price.EffectiveFrom);

            if (overlap)
            {
                ModelState.AddModelError(string.Empty,
                    "Another price already exists for this space in the selected time range. Please choose a non-overlapping period.");
                PopulateViewBags(price.SpaceId);
                return View(price);
            }

            try
            {
                _context.Update(price);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Price updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PriceExists(price.PriceId)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        PopulateViewBags(price.SpaceId);
        return View(price);
    }

    // GET: Prices/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var price = await _context.Prices.Include(p => p.Space).FirstOrDefaultAsync(p => p.PriceId == id);
        if (price == null) return NotFound();
        return View(price);
    }

    // POST: Prices/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var price = await _context.Prices.FindAsync(id);
        if (price != null)
        {
            _context.Prices.Remove(price);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Price deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
    private void PopulateViewBags(int? selectedSpaceId = null)
    {
        ViewBag.SpaceId = new SelectList(
            _context.Spaces.OrderBy(s => s.SpaceName),
            "SpaceId",
            "SpaceName",
            selectedSpaceId);
    }
    private bool PriceExists(int id)
    {
        return _context.Prices.Any(e => e.PriceId == id);
    }
}
