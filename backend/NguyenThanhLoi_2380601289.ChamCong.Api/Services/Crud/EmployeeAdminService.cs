using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Messaging;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Rbac;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud;

/// <summary>Admin tạo / sửa / xóa nhân viên. Đọc và đổi role nằm ở <see cref="RbacService"/>.</summary>
public class EmployeeAdminService(AppDbContext db)
{
    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest req, CancellationToken ct = default)
    {
        var role = req.Role ?? throw AppException.BadRequest("Thieu role");
        var username = req.Username.Trim();
        var email = NormalizeEmail(req.Email);
        var phone = string.IsNullOrWhiteSpace(req.Phone) ? null : PhoneNormalizer.Normalize(req.Phone);
        if (email is null && phone is null) throw AppException.BadRequest("Can it nhat Email hoac So dien thoai");

        await EnsureUniqueAsync(null, username, email, phone, ct);
        var position = await ResolvePositionAsync(role, req.PositionId, ct);
        await EnsureDepartmentAsync(req.DepartmentId, ct);

        // Admin tạo hộ nên coi liên hệ đã nhập là đã xác nhận (đăng nhập cần ít nhất 1 kênh đã xác nhận)
        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FullName = req.FullName.Trim(),
            Email = email, EmailVerified = email != null,
            PhoneNumber = phone, PhoneVerified = phone != null,
            Role = role,
            PositionId = position?.Id, Position = position,
            DepartmentId = req.DepartmentId,
            Status = UserStatus.Active,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return RbacService.ToDto(user);
    }

    public async Task<EmployeeDto> UpdateAsync(int actorId, int id, UpdateEmployeeRequest req, CancellationToken ct = default)
    {
        var user = await db.Users.Include(u => u.Position).FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw AppException.NotFound("Khong tim thay nhan vien");
        var email = NormalizeEmail(req.Email);
        var phone = string.IsNullOrWhiteSpace(req.Phone) ? null : PhoneNormalizer.Normalize(req.Phone);
        if (email is null && phone is null) throw AppException.BadRequest("Can it nhat Email hoac So dien thoai");

        await EnsureUniqueAsync(id, null, email, phone, ct);
        await EnsureDepartmentAsync(req.DepartmentId, ct);

        if (req.Status is { } status && status != user.Status)
        {
            if (status == UserStatus.Pending) throw AppException.BadRequest("Chi dat duoc Active hoac Locked");
            if (status == UserStatus.Locked)
            {
                if (actorId == id) throw AppException.Forbidden("Khong the tu khoa tai khoan cua chinh minh");
                await EnsureNotLastAdminAsync(user, ct);
            }
            user.Status = status;
        }

        // Đổi kênh liên hệ thì phải xác nhận lại; admin sửa nên coi là đã xác nhận
        if (email != user.Email) { user.Email = email; user.EmailVerified = email != null; }
        if (phone != user.PhoneNumber) { user.PhoneNumber = phone; user.PhoneVerified = phone != null; }

        user.FullName = req.FullName.Trim();
        user.DepartmentId = req.DepartmentId;
        if (!string.IsNullOrEmpty(req.NewPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

        await db.SaveChangesAsync(ct);
        return RbacService.ToDto(user);
    }

    public async Task DeleteAsync(int actorId, int id, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw AppException.NotFound("Khong tim thay nhan vien");
        if (actorId == id) throw AppException.Forbidden("Khong the xoa chinh minh");
        await EnsureNotLastAdminAsync(user, ct);

        var att = await db.Attendances.CountAsync(a => a.UserId == id, ct);
        if (att > 0) throw AppException.Conflict($"Nhan vien co {att} ban ghi cham cong, hay khoa tai khoan thay vi xoa");
        var leave = await db.LeaveRequests.CountAsync(l => l.UserId == id || l.ApprovedBy == id, ct);
        if (leave > 0) throw AppException.Conflict($"Nhan vien co {leave} don nghi phep lien quan, hay khoa tai khoan thay vi xoa");
        if (await db.Departments.AnyAsync(d => d.ManagerId == id, ct))
            throw AppException.Conflict("Nhan vien dang la truong phong, hay doi truong phong truoc khi xoa");

        // OTP của chính user này không có giá trị nghiệp vụ
        db.OtpCodes.RemoveRange(db.OtpCodes.Where(o => o.UserId == id));
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureNotLastAdminAsync(User target, CancellationToken ct)
    {
        if (target.Role != Role.Admin || target.Status != UserStatus.Active) return;
        var admins = await db.Users.CountAsync(u => u.Role == Role.Admin && u.Status == UserStatus.Active, ct);
        if (admins <= 1) throw AppException.Conflict("Khong the khoa/xoa Admin cuoi cung");
    }

    private async Task EnsureUniqueAsync(int? selfId, string? username, string? email, string? phone, CancellationToken ct)
    {
        if (username != null && await db.Users.AnyAsync(u => u.Username == username && u.Id != selfId, ct))
            throw AppException.Conflict("Username da ton tai");
        if (email != null && await db.Users.AnyAsync(u => u.Email == email && u.Id != selfId, ct))
            throw AppException.Conflict("Email da duoc dung");
        if (phone != null && await db.Users.AnyAsync(u => u.PhoneNumber == phone && u.Id != selfId, ct))
            throw AppException.Conflict("So dien thoai da duoc dung");
    }

    private async Task<Position?> ResolvePositionAsync(Role role, int? positionId, CancellationToken ct)
    {
        if (role == Role.Admin)
        {
            if (positionId != null) throw AppException.BadRequest("Admin khong co chuc vu");
            return null;
        }
        if (positionId == null) throw AppException.BadRequest("Thieu chuc vu");
        var p = await db.Positions.FindAsync([positionId.Value], ct)
            ?? throw AppException.BadRequest("Chuc vu khong ton tai");
        if (p.RoleGroup != role) throw AppException.BadRequest($"Chuc vu '{p.Name}' khong thuoc nhom {role}");
        return p;
    }

    private async Task EnsureDepartmentAsync(int? departmentId, CancellationToken ct)
    {
        if (departmentId != null && !await db.Departments.AnyAsync(d => d.Id == departmentId, ct))
            throw AppException.BadRequest("Phong ban khong ton tai");
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var e = email.Trim();
        try
        {
            var addr = new MailAddress(e);
            if (addr.Address != e || !addr.Host.Contains('.')) throw new FormatException();
        }
        catch (FormatException)
        {
            throw AppException.BadRequest("Email khong hop le");
        }
        return e.ToLowerInvariant();
    }
}
