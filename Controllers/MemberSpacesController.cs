using CoworkingSpace.Web.Data;
using CoworkingSpace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoworkingSpace.Web.Controllers;

[Authorize(Roles = "Member")]
public class MemberSpacesController : Controller
{
    private readonly ApplicationDbContext _context;

    public MemberSpacesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: MemberSpaces/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var space = await _context.Spaces
            .Include(s => s.Images.OrderBy(i => i.DisplayOrder))
            .Include(s => s.SpaceEquipments).ThenInclude(se => se.Equipment)
            .Include(s => s.Prices)
            .FirstOrDefaultAsync(s => s.SpaceId == id);

        if (space == null) return NotFound();

        // قیمت فعلی (آخرین قیمت معتبر) را برای نمایش به ممبر محاسبه می‌کنیم
        var currentPrice = space.Prices?
            .Where(p => p.EffectiveFrom <= DateTime.Now && p.EffectiveTo >= DateTime.Now)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefault();

        ViewBag.CurrentPrice = currentPrice?.PricePerHour;

        return View(space);
    }
}