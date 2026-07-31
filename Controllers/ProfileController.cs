using CoworkingSpace.Web.Data;
using CoworkingSpace.Web.Models;
using CoworkingSpace.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoworkingSpace.Web.Controllers;

[Authorize(Roles = "Member")]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    // GET: Profile
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member == null) return NotFound();

        var viewModel = new ProfileViewModel
        {
            User = user,
            Member = member
        };

        return View(viewModel);
    }

    // POST: Profile/ChangeFullName
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeFullName(string fullName, string currentPassword)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            TempData["Error"] = "Full name is required.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // بررسی رمز فعلی
        var passwordCheck = await _userManager.CheckPasswordAsync(user, currentPassword);
        if (!passwordCheck)
        {
            TempData["Error"] = "Current password is incorrect.";
            return RedirectToAction(nameof(Index));
        }

        // پیدا کردن Member و به‌روزرسانی FullName
        var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member == null) return NotFound();

        member.FullName = fullName.Trim();
        _context.Update(member);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Full name updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Profile/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "New password and confirmation do not match.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["Error"] = "Password must be at least 6 characters.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            TempData["Success"] = "Password changed successfully.";
        }
        else
        {
            var error = result.Errors.FirstOrDefault();
            TempData["Error"] = error?.Description ?? "Failed to change password.";
        }

        return RedirectToAction(nameof(Index));
    }
}