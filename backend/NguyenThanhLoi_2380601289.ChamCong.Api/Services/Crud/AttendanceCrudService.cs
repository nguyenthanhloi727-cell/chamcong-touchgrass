using Microsoft.EntityFrameworkCore;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud;

/// <summary>Admin xem/sửa/xóa tất cả; Manager xem/sửa phòng mình; nhân viên chỉ xem của mình.</summary>
public class AttendanceCrudService(AppDbContext db)
{
    private async Task<(User Actor, IQueryable<Attendance> Scope)> ScopeAsync(int actorId, CancellationToken ct)
    {
        var actor = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorId, ct)
            ?? throw AppException.Unauthorized("Tai khoan khong ton tai");
        var q = db.Attendances.Include(a => a.User).Include(a => a.Shift).AsQueryable();
        q = actor.Role switch
        {
            Role.Admin => q,
            Role.Manager when actor.DepartmentId != null => q.Where(a => a.User!.DepartmentId == actor.DepartmentId),
            Role.Manager => q.Where(a => a.UserId == actor.Id),
            _ => q.Where(a => a.UserId == actor.Id),
        };
        return (actor, q);
    }

    public async Task<List<AttendanceDto>> ListAsync(int actorId, string? q, int? userId, DateOnly? from, DateOnly? to,
        AttendanceStatus? status, CancellationToken ct)
    {
        var (_, scope) = await ScopeAsync(actorId, ct);
        scope = scope.AsNoTracking();
        if (userId != null) scope = scope.Where(a => a.UserId == userId);
        if (status != null) scope = scope.Where(a => a.Status == status);
        if (from != null) { var f = from.Value.ToDateTime(TimeOnly.MinValue); scope = scope.Where(a => a.CheckInAt >= f); }
        if (to != null) { var t = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue); scope = scope.Where(a => a.CheckInAt < t); }
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            scope = scope.Where(a => a.User!.Username.Contains(q) || a.User!.FullName.Contains(q));
        }
        return (await scope.OrderByDescending(a => a.CheckInAt).ThenByDescending(a => a.Id).ToListAsync(ct)).Select(ToDto).ToList();
    }

    public async Task<AttendanceDto> GetAsync(int actorId, int id, CancellationToken ct)
    {
        var (_, scope) = await ScopeAsync(actorId, ct);
        var a = await scope.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Khong tim thay ban ghi cham cong");
        return ToDto(a);
    }

    /// <summary>Admin/Manager nhập tay (vd quên chấm công). Manager chỉ nhập cho nhân viên phòng mình.</summary>
    public async Task<AttendanceDto> CreateAsync(int actorId, CreateAttendanceRequest req, CancellationToken ct)
    {
        var (actor, _) = await ScopeAsync(actorId, ct);
        var target = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == req.UserId, ct)
            ?? throw AppException.BadRequest("Nhan vien khong ton tai");
        EnsureCanManage(actor, target);
        var checkIn = req.CheckInAt ?? throw AppException.BadRequest("Thieu gio vao");
        await EnsureTimesAndShiftAsync(checkIn, req.CheckOutAt, req.ShiftId, ct);

        var a = new Attendance
        {
            UserId = target.Id, ShiftId = req.ShiftId, CheckInAt = checkIn, CheckOutAt = req.CheckOutAt,
            Lat = req.Lat, Lng = req.Lng, Status = req.Status,
        };
        db.Attendances.Add(a);
        await db.SaveChangesAsync(ct);
        return await GetAsync(actorId, a.Id, ct);
    }

    public async Task<AttendanceDto> UpdateAsync(int actorId, int id, UpdateAttendanceRequest req, CancellationToken ct)
    {
        var (actor, scope) = await ScopeAsync(actorId, ct);
        var a = await scope.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Khong tim thay ban ghi cham cong");
        EnsureCanManage(actor, a.User!);

        var checkIn = req.CheckInAt ?? a.CheckInAt;
        var checkOut = req.CheckOutAt ?? a.CheckOutAt;
        var shiftId = req.ShiftId ?? a.ShiftId;
        await EnsureTimesAndShiftAsync(checkIn, checkOut, shiftId, ct);

        a.Status = req.Status ?? throw AppException.BadRequest("Thieu trang thai");
        a.CheckInAt = checkIn;
        a.CheckOutAt = checkOut;
        a.ShiftId = shiftId;
        await db.SaveChangesAsync(ct);
        return await GetAsync(actorId, id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var a = await db.Attendances.FindAsync([id], ct) ?? throw AppException.NotFound("Khong tim thay ban ghi cham cong");
        db.Attendances.Remove(a);
        await db.SaveChangesAsync(ct);
    }

    private static void EnsureCanManage(User actor, User target)
    {
        if (actor.Role == Role.Admin) return;
        if (actor.Role == Role.Manager && actor.DepartmentId != null && actor.DepartmentId == target.DepartmentId) return;
        throw AppException.Forbidden("Khong co quyen voi nhan vien nay");
    }

    private async Task EnsureTimesAndShiftAsync(DateTime checkIn, DateTime? checkOut, int? shiftId, CancellationToken ct)
    {
        if (checkOut != null && checkOut <= checkIn) throw AppException.BadRequest("Gio ra phai sau gio vao");
        if (shiftId != null && !await db.Shifts.AnyAsync(s => s.Id == shiftId, ct))
            throw AppException.BadRequest("Ca lam khong ton tai");
    }

    private static AttendanceDto ToDto(Attendance a) => new(
        a.Id, a.UserId, a.User!.Username, a.User.FullName, a.ShiftId, a.Shift?.Name,
        a.CheckInAt, a.CheckOutAt, a.Lat, a.Lng, a.IsMocked, a.QrValid, a.FaceMatched, a.TrustScore, a.Status.ToString());
}
