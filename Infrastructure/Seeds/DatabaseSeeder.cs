using Core.Entities;
using Core.Entities.Identity;
using Core.Entities.Location;
using Core.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Seeds
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            await SeedLocationsAsync(db);
            await SeedDepartmentsAsync(db);
            await SeedCategoriesAsync(db);
            await SeedSlaPoliciesAsync(db);
            await SeedAdminUserAsync(db);
        }

        private static async Task SeedLocationsAsync(AppDbContext db)
        {
            if (await db.Districts.AnyAsync()) return;

            var district = new District { Name = "Bengaluru Urban", Code = "BLR-U" };
            db.Districts.Add(district);
            await db.SaveChangesAsync();

            var constituency = new Constituency
            {
                Name = "Shivajinagar",
                Code = "SJN",
                DistrictId = district.Id
            };
            db.Constituencies.Add(constituency);
            await db.SaveChangesAsync();

            var area = new Area
            {
                Name = "Central Area",
                Code = "BLR-C",
                ConstituencyId = constituency.Id
            };
            db.Areas.Add(area);
            await db.SaveChangesAsync();

            var blocks = new[]
            {
            new Block { Name = "Block A", Code = "BLK-A", AreaId = area.Id },
            new Block { Name = "Block B", Code = "BLK-B", AreaId = area.Id },
            new Block { Name = "Block C", Code = "BLK-C", AreaId = area.Id }
        };
            db.Blocks.AddRange(blocks);
            await db.SaveChangesAsync();
        }

        private static async Task SeedDepartmentsAsync(AppDbContext db)
        {
            if (await db.Departments.AnyAsync()) return;

            var departments = new[]
            {
            new Department { Name = "Public Works", Code = "PWD", Description = "Roads, bridges, buildings" },
            new Department { Name = "Water Supply", Code = "BWSSB", Description = "Water and sewerage" },
            new Department { Name = "Electricity", Code = "BESCOM", Description = "Power supply" },
            new Department { Name = "Health", Code = "BBMP-H", Description = "Public health services" },
            new Department { Name = "Sanitation", Code = "BBMP-S", Description = "Waste management" }
        };
            db.Departments.AddRange(departments);
            await db.SaveChangesAsync();
        }

        private static async Task SeedCategoriesAsync(AppDbContext db)
        {
            if (await db.ComplaintCategories.AnyAsync()) return;

            var pwd = await db.Departments.FirstAsync(x => x.Code == "PWD");
            var bwssb = await db.Departments.FirstAsync(x => x.Code == "BWSSB");
            var bescom = await db.Departments.FirstAsync(x => x.Code == "BESCOM");
            var health = await db.Departments.FirstAsync(x => x.Code == "BBMP-H");
            var sanitation = await db.Departments.FirstAsync(x => x.Code == "BBMP-S");

            var categories = new[]
            {
            new ComplaintCategory { Name = "Pothole / Road Damage", DepartmentId = pwd.Id },
            new ComplaintCategory { Name = "Street Light Failure", DepartmentId = bescom.Id },
            new ComplaintCategory { Name = "Water Supply Disruption", DepartmentId = bwssb.Id },
            new ComplaintCategory { Name = "Sewage Overflow", DepartmentId = bwssb.Id },
            new ComplaintCategory { Name = "Garbage Not Collected", DepartmentId = sanitation.Id },
            new ComplaintCategory { Name = "Illegal Dumping", DepartmentId = sanitation.Id },
            new ComplaintCategory { Name = "Mosquito Breeding", DepartmentId = health.Id },
        };
            db.ComplaintCategories.AddRange(categories);
            await db.SaveChangesAsync();
        }

        private static async Task SeedSlaPoliciesAsync(AppDbContext db)
        {
            if (await db.SlaPolicies.AnyAsync()) return;

            var categories = await db.ComplaintCategories.ToListAsync();
            var policies = categories.Select(c => new SlaPolicy
            {
                CategoryId = c.Id,
                ResponseHours = 4,
                ResolutionHours = 48,
                EscalationLevel1Hours = 24,
                EscalationLevel2Hours = 72,
                EscalationLevel3Hours = 120,
                IsActive = true
            });
            db.SlaPolicies.AddRange(policies);
            await db.SaveChangesAsync();
        }

        private static async Task SeedAdminUserAsync(AppDbContext db)
        {
            if (await db.InternalUsers.AnyAsync()) return;

            var admin = new InternalUser
            {
                FullName = "System Administrator",
                Email = "admin@citizenconnect.in",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = UserRole.Admin,
                IsActive = true
            };
            db.InternalUsers.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}
