namespace CoworkingSpace.Web.Models;

/// <summary>
/// All enumerations used across the co-working domain.
/// They are stored as strings in the database (varchar/nvarchar)
/// so the schema (reservation_status, payment_method, etc.) is respected.
/// </summary>
public enum ReservationStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}

public enum PaymentMethod
{
    Cash,
    Card,
    Online
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

public enum StaffRole
{
    Manager,
    Receptionist,
    Cleaner,
    Security
}

public enum MaintenanceType
{
    Cleaning,
    Repair,
    Renovation
}

public enum MaintenanceStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

public enum SpaceType
{
    MeetingRoom,
    ConferenceHall,
    PrivateOffice,
    HotDesk,
    DedicatedDesk,
    EventSpace
}
