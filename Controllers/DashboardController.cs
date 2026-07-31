using CoworkingSpace.Web.Data;
using CoworkingSpace.Web.Models;
using CoworkingSpace.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoworkingSpace.Web.Controllers;

/// <summary>
/// Admin/Staff dashboard with headline KPIs.
/// </summary>
[Authorize(Roles = "Admin,Staff")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var model = new DashboardViewModel
        {
            TotalMembers = await _context.Members.CountAsync(),
            TotalSpaces = await _context.Spaces.CountAsync(),
            TotalEquipment = await _context.Equipment.CountAsync(),
            TotalStaff = await _context.Staff.CountAsync(),
            TotalReservations = await _context.Reservations.CountAsync(),

            // Active reservations for today (any reservation that spans "now").
            ActiveReservationsToday = await _context.Reservations
                .CountAsync(r => r.StartTime <= now
                              && r.EndTime >= now
                              && (r.ReservationStatus == ReservationStatus.Pending
                                  || r.ReservationStatus == ReservationStatus.Confirmed)),

            PendingReservations = await _context.Reservations
                .CountAsync(r => r.ReservationStatus == ReservationStatus.Pending),

            // Sum of paid payments for the current month.
            RevenueThisMonth = await _context.Payments
                .Where(p => p.PaymentStatus == PaymentStatus.Paid
                         && p.PaymentDate >= monthStart)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m,

            RecentReservations = await _context.Reservations
                .Include(r => r.Member)
                .Include(r => r.Space)
                .OrderByDescending(r => r.CreatedAt)
                .Take(8)
                .ToListAsync()
        };

        return View(model);
    }
}
