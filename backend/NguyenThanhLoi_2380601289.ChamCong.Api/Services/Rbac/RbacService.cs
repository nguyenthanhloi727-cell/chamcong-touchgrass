using Microsoft.EntityFrameworkCore;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Rbac;

/// <summary>Phân quyền 2 ô (Role + Chức vụ), bảo vệ admin, phạm vi xem theo phòng ban.</summary>
public class RbacService(AppDbContext db)
{
    public async Task<EmployeeDto> ChangeRoleAsync(int actorId, int targetId, ChangeRoleRequest req, CancellationToken ct = default)
    {
        var role = req.Role ?? throw AppException.BadRequest("Thieu role");
        var target = await db.Users.Include(u => u.Position).FirstOrDefaultAsync(u => u.Id == targetId, ct)
            ?? throw AppException.NotFound("Khong tim thay nhan vien");

        // Bảo vệ admin
        if (actorId == target.Id && target.Role == Role.Admin && role != Role.Admin)
            throw AppException.Forbidden("Khong the tu ha quyen cua chinh minh");
        if (actorId == target.Id && target.Role != Role.Admin && role == Role.Admin)
            throw AppException.Forbidden("Khong the tu nang minh len Admin");
        if (target.Role == Role.Admin && role != Role.Admin)
        {
            var admins = await db.Users.CountAsync(u => u.Role == Role.Admin && u.Status == UserStatus.Active, ct);
            if (admins <= 1)
                throw AppException.Conflict("Khong the ha quyen Admin cuoi cung");
        }

        // Ô 2 phải khớp ô 1
        Position? position = null;
        if (role == Role.Admin)
        {
            if (req.PositionId != null) throw AppException.BadRequest("Admin khong co chuc vu");
        }
        else
        {
            if (req.PositionId == null) throw AppException.BadRequest("Thieu chuc vu");
            position = await db.Positions.FindAsync([req.PositionId.Value], ct)
                ?? throw AppException.BadRequest("Chuc vu khong ton tai");
            if (position.RoleGroup != role)
                throw AppException.BadRequest($"Chuc vu '{position.Name}' khong thuoc nhom {role}");
        }

        target.Role = role;
        target.PositionId = position?.Id;
        target.Position = position;
        await db.SaveChangesAsync(ct);
        return ToDto(target);
    }

    /// <summary>Admin thấy tất cả; Manager chỉ thấy cùng DepartmentId (chưa có phòng ban → chỉ thấy chính mình).</summary>
    public async Task<IQueryable<User>> VisibleUsersAsync(int actorId, CancellationToken ct = default)
    {
        var actor = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorId, ct)
            ?? throw AppException.Unauthorized("Tai khoan khong ton tai");
        var q = db.Users.AsNoTracking().Include(u => u.Position).AsQueryable();
        return actor.Role switch
        {
            Role.Admin => q,
            Role.Manager when actor.DepartmentId != null => q.Where(u => u.DepartmentId == actor.DepartmentId),
            Role.Manager => q.Where(u => u.Id == actor.Id),
            _ => throw AppException.Forbidden("Khong co quyen xem danh sach nhan vien"),
        };
    }

    public async Task<List<EmployeeDto>> ListEmployeesAsync(int actorId, string? q, CancellationToken ct = default)
    {
        var users = await VisibleUsersAsync(actorId, ct);
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            users = users.Where(u => u.Username.Contains(q) || u.FullName.Contains(q));
        }
        return (await users.OrderBy(u => u.Id).ToListAsync(ct)).Select(ToDto).ToList();
    }

    public async Task<EmployeeDto> GetEmployeeAsync(int actorId, int id, CancellationToken ct = default)
    {
        var users = await VisibleUsersAsync(actorId, ct);
        var u = await users.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Khong tim thay nhan vien");
        return ToDto(u);
    }

    public async Task<List<PositionDto>> ListPositionsAsync(Role? roleGroup, CancellationToken ct = default)
    {
        var q = db.Positions.AsNoTracking().AsQueryable();
        if (roleGroup != null) q = q.Where(p => p.RoleGroup == roleGroup);
        return await q.OrderBy(p => p.Id).Select(p => new PositionDto(p.Id, p.Name, p.RoleGroup.ToString())).ToListAsync(ct);
    }

    public static EmployeeDto ToDto(User u) => new(
        u.Id, u.Username, u.FullName, u.Email, u.PhoneNumber, u.Role.ToString(),
        u.PositionId, u.Position?.Name, u.DepartmentId, u.Status.ToString());
}
