using CoworkingSpace.Web.Models;

namespace CoworkingSpace.Web.ViewModels;

/// <summary>
/// Aggregated statistics for the admin/staff dashboard.
/// </summary>
public class DashboardViewModel
{
    public int TotalMembers { get; set; }
    public int TotalSpaces { get; set; }
    public int ActiveReservationsToday { get; set; }
    public decimal RevenueThisMonth { get; set; }

    public int PendingReservations { get; set; }
    public int TotalReservations { get; set; }
    public int TotalEquipment { get; set; }
    public int TotalStaff { get; set; }

    public IEnumerable<Reservation> RecentReservations { get; set; } = new List<Reservation>();
}
