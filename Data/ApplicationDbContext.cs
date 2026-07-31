using CoworkingSpace.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace CoworkingSpace.Web.Data;

/// <summary>
/// Application data context. Extends Identity so the AspNetXxx tables are
/// created with their default schema, and adds all co-working domain tables.
/// Fluent configuration mirrors the provided SQL schema exactly
/// (table/column names, types, keys and relationships).
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Member> Members { get; set; } = null!;
    public DbSet<Space> Spaces { get; set; } = null!;
    public DbSet<SpaceImage> SpaceImages { get; set; }
    public DbSet<Equipment> Equipment { get; set; } = null!;
    public DbSet<SpaceEquipment> SpaceEquipment { get; set; } = null!;
    public DbSet<Price> Prices { get; set; } = null!;
    public DbSet<Reservation> Reservations { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Staff> Staff { get; set; } = null!;
    public DbSet<SpaceMaintenance> SpaceMaintenances { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ---- Table names must match the provided schema exactly ----
        builder.Entity<Space>().ToTable("Spaces");
        builder.Entity<Equipment>().ToTable("Equipment");
        builder.Entity<SpaceEquipment>().ToTable("SpaceEquipment");
        builder.Entity<Price>().ToTable("Price");
        builder.Entity<Member>().ToTable("Member");
        builder.Entity<Reservation>().ToTable("Reservation");
        builder.Entity<Payment>().ToTable("Payment");
        builder.Entity<Staff>().ToTable("Staff");
        builder.Entity<SpaceMaintenance>().ToTable("SpaceMaintenance");

        // ---- Enum -> string conversions (stored in varchar/nvarchar columns) ----
        builder.Entity<Reservation>()
            .Property(r => r.ReservationStatus)
            .HasConversion<string>();

        builder.Entity<Payment>()
            .Property(p => p.PaymentMethod)
            .HasConversion<string>();

        builder.Entity<Payment>()
            .Property(p => p.PaymentStatus)
            .HasConversion<string>();

        /*builder.Entity<Staff>()
            .Property(s => s.Role)
            .HasConversion<string>();
        */
        builder.Entity<Space>()
            .Property(s => s.SpaceType)
            .HasConversion<string>();

        builder.Entity<SpaceMaintenance>()
            .Property(m => m.MaintenanceType)
            .HasConversion<string>();

        builder.Entity<SpaceMaintenance>()
            .Property(m => m.Status)
            .HasConversion<string>();

        // ---- SpaceImage Configuration ----
        builder.Entity<SpaceImage>()
            .HasOne(si => si.Space)
            .WithMany(s => s.Images)
            .HasForeignKey(si => si.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- SpaceEquipment: composite key (space_id, equipment_id) ----
        builder.Entity<SpaceEquipment>()
            .HasKey(se => new { se.SpaceId, se.EquipmentId });

        builder.Entity<SpaceEquipment>()
            .Property(se => se.Quantity)
            .HasDefaultValue((short)1);

        builder.Entity<SpaceEquipment>()
            .HasOne(se => se.Space)
            .WithMany(s => s.SpaceEquipments)
            .HasForeignKey(se => se.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SpaceEquipment>()
            .HasOne(se => se.Equipment)
            .WithMany(e => e.SpaceEquipments)
            .HasForeignKey(se => se.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- Price -> Space ----
        builder.Entity<Price>()
            .HasOne(p => p.Space)
            .WithMany(s => s.Prices)
            .HasForeignKey(p => p.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- Reservation ----
        builder.Entity<Reservation>()
            .HasOne(r => r.Member)
            .WithMany(m => m.Reservations)
            .HasForeignKey(r => r.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Reservation>()
            .HasOne(r => r.Space)
            .WithMany(s => s.Reservations)
            .HasForeignKey(r => r.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- Payment: one-to-one with Reservation (reservation_id UNIQUE) ----
        builder.Entity<Reservation>()
            .HasOne(r => r.Payment)
            .WithOne(p => p.Reservation)
            .HasForeignKey<Payment>(p => p.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Payment>()
            .HasIndex(p => p.ReservationId)
            .IsUnique();

        // Price -> Payment: Restrict to avoid multiple cascade paths (SQL Server).
        builder.Entity<Payment>()
            .HasOne(p => p.Price)
            .WithMany(pr => pr.Payments)
            .HasForeignKey(p => p.PriceId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Member -> AspNetUsers (optional link) ----
        builder.Entity<Member>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- SpaceMaintenance ----
        builder.Entity<SpaceMaintenance>()
            .ToTable("SpaceMaintenance");

        builder.Entity<SpaceMaintenance>()
            .HasOne(m => m.Space)
            .WithMany(s => s.Maintenances)
            .HasForeignKey(m => m.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SpaceMaintenance>()
            .HasOne(m => m.Staff)
            .WithMany(s => s.Maintenances)
            .HasForeignKey(m => m.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Precision for decimals ----
        builder.Entity<Price>()
            .Property(p => p.PricePerHour)
            .HasColumnType("decimal(10,2)");

        builder.Entity<Reservation>()
            .Property(r => r.AppliedPricePerHour)
            .HasColumnType("decimal(10,2)");

        builder.Entity<Reservation>()
            .Property(r => r.TotalAmount)
            .HasColumnType("decimal(10,2)");

        builder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(10,2)");
    }
}