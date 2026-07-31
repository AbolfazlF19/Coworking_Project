using CoworkingSpace.Web.Data;
using CoworkingSpace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoworkingSpace.Web.Controllers;

/// <summary>
/// Admin management of <see cref="Member"/> records.
/// Members are created automatically through public registration, but an
/// Admin can also manage them here (create / edit / delete) and optionally
/// link a record to an existing Identity account.
/// </summary>
[Authorize(Roles = "Admin")]
public class MembersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MembersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Members
    public async Task<IActionResult> Index()
    {
        var members = await _context.Members
            .Include(m => m.User)
            .OrderByDescending(m => m.RegisterDate)
            .ToListAsync();
        return View(members);
    }

    // GET: Members/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var member = await _context.Members
            .Include(m => m.User)
            .Include(m => m.Reservations).ThenInclude(r => r.Space)
            .FirstOrDefaultAsync(m => m.MemberId == id);

        if (member == null) return NotFound();
        return View(member);
    }

    // GET: Members/Create
    public async Task<IActionResult> Create()
    {
        await PopulateUserDropdownAsync();
        var member = new Member { RegisterDate = DateTime.Today };
        return View(member);
    }

    // POST: Members/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FullName,Phone,Email,RegisterDate,UserId")] Member member)
    {
        ModelState.Remove("User");

        if (await _context.Members.AnyAsync(m => m.Email == member.Email))
        {
            ModelState.AddModelError("Email", "A member with this email already exists.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(member);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Member created.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateUserDropdownAsync(member.UserId);
        return View(member);
    }

    // GET: Members/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var member = await _context.Members.FindAsync(id);
        if (member == null) return NotFound();

        await PopulateUserDropdownAsync(member.UserId);
        return View(member);
    }

    // POST: Members/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("MemberId,FullName,Phone,Email,RegisterDate,UserId")] Member member)
    {
        if (id != member.MemberId) return NotFound();
        ModelState.Remove("User");

        if (await _context.Members.AnyAsync(m => m.Email == member.Email && m.MemberId != id))
        {
            ModelState.AddModelError("Email", "A member with this email already exists.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(member);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Member updated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Members.AnyAsync(m => m.MemberId == member.MemberId)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        await PopulateUserDropdownAsync(member.UserId);
        return View(member);
    }

    // GET: Members/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var member = await _context.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.MemberId == id);

        if (member == null) return NotFound();
        return View(member);
    }

    // POST: Members/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var member = await _context.Members.FindAsync(id);
        if (member != null)
        {
            _context.Members.Remove(member);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Member deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Lists Identity users in the Member role that are not yet linked to a
    /// Member record (so an Admin can attach an account), plus the currently
    /// selected one if any.
    /// </summary>
    private async Task PopulateUserDropdownAsync(string? selectedUserId = null)
    {
        var memberRoleUsers = await _userManager.GetUsersInRoleAsync("Member");
        var linkedUserIds = await _context.Members
            .Where(m => m.UserId != null)
            .Select(m => m.UserId!)
            .ToListAsync();

        var available = memberRoleUsers
            .Where(u => !linkedUserIds.Contains(u.Id) || u.Id == selectedUserId)
            .OrderBy(u => u.Email)
            .Select(u => new SelectListItem { Value = u.Id, Text = u.Email });

        ViewBag.UserId = new SelectList(available, "Value", "Text", selectedUserId);
    }
}
