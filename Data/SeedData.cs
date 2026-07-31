using CoworkingSpace.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoworkingSpace.Web.Data;

/// <summary>
/// Seeds the three roles, the default Admin account and some demo domain data
/// (spaces, equipment, prices, staff, and a demo member account).
/// Idempotent: safe to run on every application start.
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        // ---- Roles ----
        string[] roles = { "Admin", "Staff", "Member" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // ---- Default Admin user ----
        const string adminEmail = "admin@coworking.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                PhoneNumber = "000-000-0000"
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // ---- Demo Member account (for testing member flows) ----
        /*const string memberEmail = "member@coworking.com";
        var memberUser = await userManager.FindByEmailAsync(memberEmail);
        if (memberUser == null)
        {
            memberUser = new ApplicationUser
            {
                UserName = memberEmail,
                Email = memberEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(memberUser, "Member@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(memberUser, "Member");

                if (!await context.Members.AnyAsync(m => m.UserId == memberUser.Id))
                {
                    context.Members.Add(new Member
                    {
                        FullName = "Demo Member",
                        Phone = "0612345678",
                        Email = memberEmail,
                        RegisterDate = DateTime.Today,
                        UserId = memberUser.Id
                    });
                    await context.SaveChangesAsync();
                }
            }
        }
        */

        // ---- Demo domain data (only when tables are empty) ----
        /*
        if (!await context.Spaces.AnyAsync())
        {
            var spaces = new List<Space>
            {
                new() { SpaceName = "Focus Room A", SpaceType = SpaceType.MeetingRoom, Capacity = 6, IsActive = true, Location = "Floor 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { SpaceName = "Grand Conference Hall", SpaceType = SpaceType.ConferenceHall, Capacity = 80, IsActive = true, Location = "Floor 2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { SpaceName = "Private Office 7", SpaceType = SpaceType.PrivateOffice, Capacity = 4, IsActive = true, Location = "Floor 3", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { SpaceName = "Hot Desk Zone", SpaceType = SpaceType.HotDesk, Capacity = 20, IsActive = true, Location = "Floor 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };
            context.Spaces.AddRange(spaces);
            await context.SaveChangesAsync();

            var equipment = new List<Equipment>
            {
                new() { Name = "Projector", Description = "Full HD ceiling mounted", Category = "Audio-Visual" },
                new() { Name = "Whiteboard", Description = "Magnetic 120x90", Category = "Stationery" },
                new() { Name = "Video Conferencing Kit", Description = "Camera + mic + speakers", Category = "Audio-Visual" },
                new() { Name = "Smart TV", Description = "65 inch 4K", Category = "Audio-Visual" }
            };
            context.Equipment.AddRange(equipment);
            await context.SaveChangesAsync();

            // Assign equipment to spaces
            context.SpaceEquipment.AddRange(
                new SpaceEquipment { SpaceId = spaces[0].SpaceId, EquipmentId = equipment[1].EquipmentId, Quantity = 1 },
                new SpaceEquipment { SpaceId = spaces[1].SpaceId, EquipmentId = equipment[0].EquipmentId, Quantity = 1 },
                new SpaceEquipment { SpaceId = spaces[1].SpaceId, EquipmentId = equipment[2].EquipmentId, Quantity = 1 },
                new SpaceEquipment { SpaceId = spaces[2].SpaceId, EquipmentId = equipment[3].EquipmentId, Quantity = 1 }
            );

            // Prices (valid for the whole current year)
            var yearStart = new DateTime(DateTime.UtcNow.Year, 1, 1);
            var yearEnd = new DateTime(DateTime.UtcNow.Year, 12, 31, 23, 59, 59);
            context.Prices.AddRange(
                new Price { SpaceId = spaces[0].SpaceId, PricePerHour = 15m, EffectiveFrom = yearStart, EffectiveTo = yearEnd },
                new Price { SpaceId = spaces[1].SpaceId, PricePerHour = 60m, EffectiveFrom = yearStart, EffectiveTo = yearEnd },
                new Price { SpaceId = spaces[2].SpaceId, PricePerHour = 25m, EffectiveFrom = yearStart, EffectiveTo = yearEnd },
                new Price { SpaceId = spaces[3].SpaceId, PricePerHour = 8m, EffectiveFrom = yearStart, EffectiveTo = yearEnd }
            );

            // Staff
            context.Staff.AddRange(
                new Staff { FullName = "Alice van der Berg", Role = StaffRole.Manager, Phone = "0610000001", Email = "alice@coworking.com" },
                new Staff { FullName = "Bob de Vries", Role = StaffRole.Receptionist, Phone = "0610000002", Email = "bob@coworking.com" },
                new Staff { FullName = "Carol Jansen", Role = StaffRole.Cleaner, Phone = "0610000003", Email = "carol@coworking.com" }
            );

            await context.SaveChangesAsync();
        }
        */
    }
}
