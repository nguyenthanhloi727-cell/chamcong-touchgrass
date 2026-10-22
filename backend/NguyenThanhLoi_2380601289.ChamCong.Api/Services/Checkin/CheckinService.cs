using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Rbac;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Checkin;

/// <summary>Check-in/out (QR + GPS + điểm tin cậy), duyệt, báo cáo tháng, chia sẻ vị trí.</summary>
public class CheckinService(AppDbContext db, QrService qr, JwtService jwt, RbacService rbac, IOptions<GeofenceOptions> geo, TimeProvider clock)
{
    // Chặn bấm đúp / gửi 2 request cùng lúc vượt qua kiểm tra trùng ca (1 tiến trình backend)
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> UserLocks = new();

    public async Task<CheckInResult> CheckInAsync(int userId, CheckInRequest req, CancellationToken ct)
    {
        var gate = UserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try { return await DoCheckInAsync(userId, req, ct); }
        finally { gate.Release(); }
    }

    private async Task<CheckInResult> DoCheckInAsync(int userId, CheckInRequest req, CancellationToken ct)
    {
        var lat = req.Lat ?? throw AppException.BadRequest("Thieu lat");
        var lng = req.Lng ?? throw AppException.BadRequest("Thieu lng");
        var now = clock.GetLocalNow().DateTime;
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var shift = await PickShiftAsync(now.TimeOfDay, ct);
        var shiftId = shift?.Id;
        var dup = await db.Attendances.AnyAsync(a => a.UserId == userId && a.CheckInAt >= today
            && a.CheckInAt < tomorrow && a.ShiftId == shiftId && a.Status != AttendanceStatus.Rejected, ct);
        if (dup) throw AppException.Conflict("Da cham cong vao ca nay hom nay");

        var (gpsOk, distance) = await CheckGeofenceAsync(lat, lng, ct);
        var qrValid = qr.IsValid(req.QrToken);
        var faceMatched = jwt.ValidateFaceToken(req.FaceToken, userId);
        var (score, b) = TrustScore.Compute(faceMatched, gpsOk, qrValid, req.IsMocked);

        var status = score < TrustScore.PassThreshold
            ? AttendanceStatus.PendingReview
            : OnTimeOrLate(shift, now);

        var a = new Attendance
        {
            UserId = userId, ShiftId = shiftId, CheckInAt = now, Lat = lat, Lng = lng,
            IsMocked = req.IsMocked, QrValid = qrValid, FaceMatched = faceMatched,
            TrustScore = score, Status = status,
        };
        db.Attendances.Add(a);
        await db.SaveChangesAsync(ct);
        return new CheckInResult(a.Id, score, new TrustBreakdownDto(b.Face, b.Gps, b.Qr, b.NotMocked),
            status.ToString(), Math.Round(distance, 1));
    }

    public async Task<AttendanceDto> CheckOutAsync(int userId, CancellationToken ct)
    {
        var now = clock.GetLocalNow().DateTime;
        var a = await db.Attendances.Include(x => x.User).Include(x => x.Shift)
            .Where(x => x.UserId == userId && x.CheckOutAt == null && x.Status != AttendanceStatus.Rejected)
            .OrderByDescending(x => x.CheckInAt).FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("Chua cham cong vao, khong the check-out");
        if (a.CheckInAt.Date != now.Date)
            throw AppException.Conflict("Ban ghi vao ca la cua ngay truoc, hay nho quan ly sua");
        a.CheckOutAt = now;
        await db.SaveChangesAsync(ct);
        return ToDto(a);
    }

    public async Task<AttendanceDto> ReviewAsync(int actorId, int id, bool approve, CancellationToken ct)
    {
        var actor = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorId, ct)
            ?? throw AppException.Unauthorized("Tai khoan khong ton tai");
        var a = await db.Attendances.Include(x => x.User).Include(x => x.Shift).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Khong tim thay ban ghi cham cong");
        if (actor.Role == Role.Employee) throw AppException.Forbidden("Khong co quyen duyet");
        if (actor.Role == Role.Manager)
        {
            if (actor.DepartmentId == null || actor.DepartmentId != a.User!.DepartmentId)
                throw AppException.Forbidden("Chi duyet duoc nhan vien phong minh");
            if (a.UserId == actor.Id) throw AppException.Forbidden("Khong the tu duyet cham cong cua chinh minh");
        }
        if (a.Status != AttendanceStatus.PendingReview)
            throw AppException.Conflict("Ban ghi khong o trang thai cho duyet");

        a.Status = approve ? OnTimeOrLate(a.Shift, a.CheckInAt) : AttendanceStatus.Rejected;
        await db.SaveChangesAsync(ct);
        return ToDto(a);
    }

    public async Task<List<MonthlyReportRow>> MonthlyReportAsync(int actorId, string? month, int? departmentId, CancellationToken ct)
    {
        if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
            throw AppException.BadRequest("month phai co dang YYYY-MM");
        var end = start.AddMonths(1);

        var actor = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorId, ct)
            ?? throw AppException.Unauthorized("Tai khoan khong ton tai");
        if (actor.Role == Role.Employee) throw AppException.Forbidden("Khong co quyen xem bao cao");
        if (actor.Role == Role.Manager && departmentId != null && departmentId != actor.DepartmentId)
            throw AppException.Forbidden("Chi xem duoc bao cao phong minh");

        var users = (await rbac.VisibleUsersAsync(actorId, ct)).Where(u => u.Role != Role.Admin && u.Status == UserStatus.Active);
        if (departmentId != null) users = users.Where(u => u.DepartmentId == departmentId);
        var list = await users.OrderBy(u => u.Id).ToListAsync(ct);
        var ids = list.Select(u => u.Id).ToList();

        var atts = await db.Attendances.AsNoTracking()
            .Where(a => ids.Contains(a.UserId) && a.CheckInAt >= start && a.CheckInAt < end)
            .Select(a => new { a.UserId, a.CheckInAt, a.Status }).ToListAsync(ct);

        return list.Select(u =>
        {
            var mine = atts.Where(a => a.UserId == u.Id).ToList();
            var worked = mine.Where(a => a.Status is AttendanceStatus.OnTime or AttendanceStatus.Late);
            return new MonthlyReportRow(u.Id, u.Username, u.FullName, u.Position?.Name,
                worked.Select(a => a.CheckInAt.Date).Distinct().Count(),
                mine.Count(a => a.Status == AttendanceStatus.Late),
                mine.Count(a => a.Status == AttendanceStatus.PendingReview),
                mine.Count(a => a.Status == AttendanceStatus.Rejected));
        }).ToList();
    }

    public async Task ShareLocationAsync(int userId, ShareLocationRequest req, CancellationToken ct)
    {
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw AppException.Unauthorized("Tai khoan khong ton tai");
        u.LastLat = req.Lat ?? throw AppException.BadRequest("Thieu lat");
        u.LastLng = req.Lng ?? throw AppException.BadRequest("Thieu lng");
        u.LastLocationAt = clock.GetLocalNow().DateTime;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Manager: nhân viên phòng mình; Admin: tất cả.</summary>
    public async Task<List<TeamLocationDto>> TeamLocationsAsync(int actorId, CancellationToken ct)
    {
        var users = await rbac.VisibleUsersAsync(actorId, ct);
        var list = await users.Where(u => u.LastLocationAt != null).OrderBy(u => u.Id).ToListAsync(ct);
        return list.Select(u => new TeamLocationDto(u.Id, u.Username, u.FullName, u.Position?.Name,
            u.LastLat!.Value, u.LastLng!.Value, u.LastLocationAt!.Value)).ToList();
    }

    // ---- helpers ----

    /// <summary>Ca đang diễn ra (cho vào sớm 2 giờ); không có thì ca bắt đầu gần giờ hiện tại nhất.</summary>
    private async Task<Shift?> PickShiftAsync(TimeSpan now, CancellationToken ct)
    {
        var shifts = await db.Shifts.AsNoTracking().OrderBy(s => s.StartTime).ThenBy(s => s.Id).ToListAsync(ct);
        return shifts.FirstOrDefault(s => now >= s.StartTime.ToTimeSpan() - TimeSpan.FromHours(2) && now <= s.EndTime.ToTimeSpan())
            ?? shifts.OrderBy(s => Math.Abs((s.StartTime.ToTimeSpan() - now).TotalMinutes)).FirstOrDefault();
    }

    private static AttendanceStatus OnTimeOrLate(Shift? shift, DateTime checkIn) =>
        shift != null && TimeOnly.FromDateTime(checkIn) > shift.StartTime ? AttendanceStatus.Late : AttendanceStatus.OnTime;

    /// <summary>Trong bán kính của bất kỳ Location nào; chưa có Location thì dùng Geofence trong cấu hình.</summary>
    private async Task<(bool Ok, double Distance)> CheckGeofenceAsync(double lat, double lng, CancellationToken ct)
    {
        var spots = await db.Locations.AsNoTracking().ToListAsync(ct);
        var fences = spots.Count > 0
            ? spots.Select(l => (l.Lat, l.Lng, R: (double)l.RadiusMeters)).ToList()
            : [(geo.Value.Lat, geo.Value.Lng, R: (double)geo.Value.RadiusMeters)];
        var best = fences.Select(f => (D: TrustScore.DistanceMeters(lat, lng, f.Lat, f.Lng), f.R))
            .OrderBy(x => x.D - x.R).First();
        return (best.D <= best.R, best.D);
    }

    private static AttendanceDto ToDto(Attendance a) => new(
        a.Id, a.UserId, a.User!.Username, a.User.FullName, a.ShiftId, a.Shift?.Name,
        a.CheckInAt, a.CheckOutAt, a.Lat, a.Lng, a.IsMocked, a.QrValid, a.FaceMatched, a.TrustScore, a.Status.ToString());
}
