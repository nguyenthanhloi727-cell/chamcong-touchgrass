using Microsoft.EntityFrameworkCore;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Data;

/// <summary>Seed lúc khởi động (không dùng HasData vì hash BCrypt mỗi lần một khác).</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration cfg, ILogger logger)
    {
        if (!await db.Positions.AnyAsync())
        {
            db.Positions.AddRange(
                new Position { Name = "Trưởng phòng Nhân sự", RoleGroup = Role.Manager },
                new Position { Name = "Quản lý Kho", RoleGroup = Role.Manager },
                new Position { Name = "Quản lý Tài chính", RoleGroup = Role.Manager },
                new Position { Name = "Kế toán", RoleGroup = Role.Employee },
                new Position { Name = "Thủ kho", RoleGroup = Role.Employee },
                new Position { Name = "Lễ tân", RoleGroup = Role.Employee },
                new Position { Name = "Bảo vệ", RoleGroup = Role.Employee },
                new Position { Name = "Lao công", RoleGroup = Role.Employee });
            await db.SaveChangesAsync();
        }

        if (!await db.Departments.AnyAsync())
        {
            db.Departments.Add(new Department { Name = "Phòng Hành chính" });
            await db.SaveChangesAsync();
        }

        if (!await db.Shifts.AnyAsync())
        {
            db.Shifts.Add(new Shift { Name = "Ca hành chính", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0) });
            await db.SaveChangesAsync();
        }

        var deptId = (await db.Departments.OrderBy(d => d.Id).FirstAsync()).Id;
        var managerPos = (await db.Positions.OrderBy(p => p.Id).FirstAsync(p => p.RoleGroup == Role.Manager)).Id;
        var employeePos = (await db.Positions.OrderBy(p => p.Id).FirstAsync(p => p.RoleGroup == Role.Employee)).Id;

        await EnsureUserAsync(db, logger, "admin", "Quản trị viên", Role.Admin, null, null,
            cfg["Seed:AdminPassword"], cfg["Seed:AdminEmail"], cfg["Seed:AdminPhone"]);
        await EnsureUserAsync(db, logger, "manager1", "Quản lý mẫu", Role.Manager, managerPos, deptId,
            cfg["Seed:ManagerPassword"], cfg["Seed:ManagerEmail"], cfg["Seed:ManagerPhone"]);
        await EnsureUserAsync(db, logger, "employee1", "Nhân viên mẫu", Role.Employee, employeePos, deptId,
            cfg["Seed:EmployeePassword"], cfg["Seed:EmployeeEmail"], cfg["Seed:EmployeePhone"]);
    }

    private static async Task EnsureUserAsync(AppDbContext db, ILogger logger,
        string username, string fullName, Role role, int? positionId, int? departmentId,
        string? password, string? email, string? phone)
    {
        if (await db.Users.AnyAsync(u => u.Username == username)) return;

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Bỏ qua seed '{User}': thiếu Seed:*Password trong appsettings.Development.json", username);
            return;
        }

        phone = NormalizePhone(phone);
        email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        var hasRealContact = email != null || phone != null;
        if (!hasRealContact)
        {
            // CHECK cần ít nhất 1 kênh; dùng email giả (chưa xác nhận) để seed không vỡ
            email = $"{username}@chamcong.local";
            logger.LogWarning("'{User}' chưa có email/SĐT thật trong Seed:* -> dùng {Email} (chưa xác nhận, chưa đăng nhập OTP được)", username, email);
        }

        db.Users.Add(new User
        {
            Username = username,
            FullName = fullName,
            Role = role,
            PositionId = positionId,
            DepartmentId = departmentId,
            Status = UserStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Email = email,
            EmailVerified = hasRealContact && email != null,
            PhoneNumber = phone,
            PhoneVerified = phone != null,
        });
        await db.SaveChangesAsync();
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        phone = phone.Trim().Replace(" ", "");
        return phone.StartsWith('0') ? "+84" + phone[1..] : phone;
    }
}
