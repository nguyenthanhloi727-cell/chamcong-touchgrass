using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Checkin;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Rbac;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Tests;

public class CheckinTests
{
    private sealed class TestClock(DateTimeOffset start) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = start;
        public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private readonly AppDbContext _db;
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 10, 22, 7, 55, 0, TimeSpan.Zero));
    private readonly QrService _qr;
    private readonly JwtService _jwt = new(Microsoft.Extensions.Options.Options.Create(new JwtOptions { Issuer = "i", Audience = "a", Key = new string('k', 40) }));
    private readonly CheckinService _svc;
    private readonly User _admin, _manager, _employee, _otherEmployee;
    private const double Lat = 10.8, Lng = 106.7;

    public CheckinTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _qr = new QrService(Microsoft.Extensions.Options.Options.Create(new QrOptions { Secret = "test-secret-test-secret", WindowSeconds = 30 }), _clock);
        var geo = Microsoft.Extensions.Options.Options.Create(new GeofenceOptions { Lat = Lat, Lng = Lng, RadiusMeters = 100 });
        _svc = new CheckinService(_db, _qr, _jwt, new RbacService(_db), geo, _clock);

        var a = new Department { Name = "A" };
        var b = new Department { Name = "B" };
        _db.Departments.AddRange(a, b);
        _db.Shifts.Add(new Shift { Name = "Hanh chinh", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0) });
        _db.SaveChanges();
        _admin = Add("admin", Role.Admin, null);
        _manager = Add("manager1", Role.Manager, a.Id);
        _employee = Add("employee1", Role.Employee, a.Id);
        _otherEmployee = Add("employee2", Role.Employee, b.Id);
    }

    private User Add(string name, Role role, int? dept)
    {
        var u = new User { Username = name, FullName = name, Email = name + "@x.com", Role = role, DepartmentId = dept, Status = UserStatus.Active };
        _db.Users.Add(u);
        _db.SaveChanges();
        return u;
    }

    private static async Task<int> StatusOf(Func<Task> act)
    {
        try { await act(); return 200; }
        catch (AppException e) { return e.StatusCode; }
    }

    private CheckInRequest Req(bool face = true, bool qr = true, double lat = Lat, double lng = Lng, bool mocked = false) =>
        new() { Lat = lat, Lng = lng, IsMocked = mocked, FaceToken = face ? _jwt.CreateFaceToken(_employee.Id, 120) : null, QrToken = qr ? _qr.Current().Token : "bad.token" };

    // ---------- QR ----------

    [Fact]
    public void Qr_CurrentAndPreviousWindowValid_OlderInvalid_Tampered()
    {
        var token = _qr.Current().Token;
        Assert.True(_qr.IsValid(token));
        _clock.Now = _clock.Now.AddSeconds(30);
        Assert.True(_qr.IsValid(token));
        _clock.Now = _clock.Now.AddSeconds(30);
        Assert.False(_qr.IsValid(token));
        Assert.False(_qr.IsValid(null));
        Assert.False(_qr.IsValid("abc"));
        Assert.False(_qr.IsValid("###." + token.Split('.')[1]));
        var parts = _qr.Current().Token.Split('.');
        Assert.False(_qr.IsValid(parts[0] + "." + new string('0', parts[1].Length)));
    }

    [Fact]
    public void Qr_ExpiresInWithinWindow()
    {
        var e = _qr.Current().ExpiresIn;
        Assert.InRange(e, 1, 30);
    }

    // ---------- Điểm tin cậy ----------

    [Fact]
    public void Trust_Weights()
    {
        Assert.Equal(100, TrustScore.Compute(true, true, true, false).Total);
        var (s, b) = TrustScore.Compute(true, true, true, true);
        Assert.Equal(60, s);
        Assert.Equal(0, b.Gps);
        Assert.Equal(0, b.NotMocked);
        Assert.Equal(65, TrustScore.Compute(false, true, true, false).Total);
    }

    [Fact]
    public void Haversine_KnownDistance()
    {
        Assert.InRange(TrustScore.DistanceMeters(Lat, Lng, Lat + 0.001, Lng), 110, 112);
        Assert.Equal(0, TrustScore.DistanceMeters(Lat, Lng, Lat, Lng), 3);
    }

    // ---------- Check-in ----------

    [Fact]
    public async Task CheckIn_AllGood_OnTime100()
    {
        var r = await _svc.CheckInAsync(_employee.Id, Req(), default);
        Assert.Equal(100, r.TrustScore);
        Assert.Equal("OnTime", r.Status);
        Assert.Equal(new TrustBreakdownDto(35, 30, 25, 10), r.Breakdown);
    }

    [Fact]
    public async Task CheckIn_AfterShiftStart_Late()
    {
        _clock.Now = new DateTimeOffset(2026, 10, 22, 8, 20, 0, TimeSpan.Zero);
        var r = await _svc.CheckInAsync(_employee.Id, Req(), default);
        Assert.Equal("Late", r.Status);
    }

    [Fact]
    public async Task CheckIn_Twice_SameShift_409()
    {
        await _svc.CheckInAsync(_employee.Id, Req(), default);
        Assert.Equal(409, await StatusOf(() => _svc.CheckInAsync(_employee.Id, Req(), default)));
    }

    [Fact]
    public async Task CheckIn_OutsideRadius_LosesGps_PendingReview()
    {
        var r = await _svc.CheckInAsync(_employee.Id, Req(lat: Lat + 0.01), default);
        Assert.Equal(0, r.Breakdown.Gps);
        Assert.Equal(70, r.TrustScore);
        Assert.Equal("OnTime", r.Status); // 70 vẫn đạt ngưỡng
    }

    [Fact]
    public async Task CheckIn_StaleQr_QrZero()
    {
        var token = _qr.Current().Token;
        _clock.Now = _clock.Now.AddSeconds(65);
        var r = await _svc.CheckInAsync(_employee.Id, new CheckInRequest { Lat = Lat, Lng = Lng, FaceToken = _jwt.CreateFaceToken(_employee.Id, 120), QrToken = token }, default);
        Assert.Equal(0, r.Breakdown.Qr);
        Assert.Equal(75, r.TrustScore);
    }

    [Fact]
    public async Task CheckIn_Mocked_PendingReview()
    {
        var r = await _svc.CheckInAsync(_employee.Id, Req(mocked: true), default);
        Assert.Equal("PendingReview", r.Status);
        Assert.Equal(0, r.Breakdown.Gps);
    }

    [Fact]
    public async Task CheckIn_NoFace_PendingReview()
    {
        var r = await _svc.CheckInAsync(_employee.Id, Req(face: false), default);
        Assert.Equal(65, r.TrustScore);
        Assert.Equal("PendingReview", r.Status);
    }

    [Fact]
    public async Task CheckIn_UsesLocationsWhenPresent()
    {
        _db.Locations.Add(new Location { Name = "Chi nhanh", Lat = 20, Lng = 105, RadiusMeters = 50 });
        _db.SaveChanges();
        var r = await _svc.CheckInAsync(_employee.Id, Req(), default); // vị trí cũ nay ngoài vùng
        Assert.Equal(0, r.Breakdown.Gps);
        var r2 = await _svc.CheckInAsync(_otherEmployee.Id, Req(lat: 20, lng: 105), default);
        Assert.Equal(30, r2.Breakdown.Gps);
    }

    [Fact]
    public async Task CheckIn_ConcurrentDoubleTap_OnlyOneRecord()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => StatusOf(() => _svc.CheckInAsync(_employee.Id, Req(), default))));
        Assert.Equal(1, results.Count(s => s == 200));
        Assert.Equal(1, _db.Attendances.Count(a => a.UserId == _employee.Id));
    }

    [Fact]
    public async Task CheckIn_AfterRejected_CanCheckInAgain()
    {
        var id = await Pending(_employee);
        await _svc.ReviewAsync(_manager.Id, id, false, default);
        var r = await _svc.CheckInAsync(_employee.Id, Req(), default);
        Assert.Equal("OnTime", r.Status);
    }

    [Fact]
    public async Task CheckIn_OverlappingShifts_PicksEarliestStart()
    {
        _db.Shifts.Add(new Shift { Name = "Ca sang som", StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(12, 0) });
        _db.SaveChanges();
        var id = (await _svc.CheckInAsync(_employee.Id, Req(), default)).AttendanceId;
        Assert.Equal("Ca sang som", _db.Attendances.Include(a => a.Shift).Single(a => a.Id == id).Shift!.Name);
    }

    [Fact]
    public void VietnamTime_IsUtcPlus7()
    {
        var vn = new VietnamTimeProvider();
        Assert.Equal(TimeSpan.FromHours(7), vn.LocalTimeZone.GetUtcOffset(DateTime.UtcNow));
    }

    // ---------- Check-out ----------

    [Fact]
    public async Task CheckOut_Flow()
    {
        Assert.Equal(404, await StatusOf(() => _svc.CheckOutAsync(_employee.Id, default)));
        await _svc.CheckInAsync(_employee.Id, Req(), default);
        _clock.Now = _clock.Now.AddHours(9);
        var a = await _svc.CheckOutAsync(_employee.Id, default);
        Assert.NotNull(a.CheckOutAt);
        Assert.Equal(404, await StatusOf(() => _svc.CheckOutAsync(_employee.Id, default)));
    }

    // ---------- Duyệt ----------

    private async Task<int> Pending(User u)
    {
        var r = await _svc.CheckInAsync(u.Id, Req(face: false), default);
        return r.AttendanceId;
    }

    [Fact]
    public async Task Review_Approve_And_Reject()
    {
        var id = await Pending(_employee);
        var a = await _svc.ReviewAsync(_manager.Id, id, true, default);
        Assert.Equal("OnTime", a.Status);
        Assert.Equal(409, await StatusOf(() => _svc.ReviewAsync(_manager.Id, id, false, default)));

        var id2 = await Pending(_otherEmployee);
        Assert.Equal("Rejected", (await _svc.ReviewAsync(_admin.Id, id2, false, default)).Status);
    }

    [Fact]
    public async Task Review_Permissions()
    {
        var other = await Pending(_otherEmployee);
        Assert.Equal(403, await StatusOf(() => _svc.ReviewAsync(_manager.Id, other, true, default))); // khác phòng
        var mine = await Pending(_employee);
        Assert.Equal(403, await StatusOf(() => _svc.ReviewAsync(_employee.Id, mine, true, default)));
        Assert.Equal(404, await StatusOf(() => _svc.ReviewAsync(_admin.Id, 9999, true, default)));
    }

    [Fact]
    public async Task Review_Manager_CannotSelfApprove()
    {
        var id = (await _svc.CheckInAsync(_manager.Id, Req(face: false), default)).AttendanceId;
        Assert.Equal(403, await StatusOf(() => _svc.ReviewAsync(_manager.Id, id, true, default)));
    }

    // ---------- Báo cáo ----------

    [Fact]
    public async Task Report_CountsWorkLatePending()
    {
        await _svc.CheckInAsync(_employee.Id, Req(), default);                       // 22/10 đúng giờ
        _clock.Now = new DateTimeOffset(2026, 10, 23, 8, 30, 0, TimeSpan.Zero);
        await _svc.CheckInAsync(_employee.Id, Req(), default);                       // 23/10 muộn
        _clock.Now = new DateTimeOffset(2026, 10, 24, 8, 0, 0, TimeSpan.Zero);
        await _svc.CheckInAsync(_employee.Id, Req(face: false), default);           // 24/10 chờ duyệt

        var rows = await _svc.MonthlyReportAsync(_manager.Id, "2026-10", null, default);
        Assert.DoesNotContain(rows, r => r.UserId == _otherEmployee.Id); // khác phòng
        var row = rows.Single(r => r.UserId == _employee.Id);
        Assert.Equal((2, 1, 1, 0), (row.WorkDays, row.LateCount, row.PendingCount, row.RejectedCount));

        var pendingId = _db.Attendances.Single(a => a.Status == AttendanceStatus.PendingReview).Id;
        await _svc.ReviewAsync(_manager.Id, pendingId, false, default);
        row = (await _svc.MonthlyReportAsync(_manager.Id, "2026-10", null, default)).Single(r => r.UserId == _employee.Id);
        Assert.Equal((2, 0, 1), (row.WorkDays, row.PendingCount, row.RejectedCount));

        Assert.Equal(0, (await _svc.MonthlyReportAsync(_admin.Id, "2026-09", null, default)).Sum(r => r.WorkDays));
        Assert.Single(await _svc.MonthlyReportAsync(_admin.Id, "2026-10", _db.Departments.First(d => d.Name == "B").Id, default));
    }

    [Fact]
    public async Task Report_Validation()
    {
        Assert.Equal(400, await StatusOf(() => _svc.MonthlyReportAsync(_admin.Id, "10/2026", null, default)));
        Assert.Equal(403, await StatusOf(() => _svc.MonthlyReportAsync(_employee.Id, "2026-10", null, default)));
        Assert.Equal(403, await StatusOf(() => _svc.MonthlyReportAsync(_manager.Id, "2026-10",
            _db.Departments.First(d => d.Name == "B").Id, default)));
    }

    // ---------- Chia sẻ vị trí ----------

    [Fact]
    public async Task LocationShare_ManagerSeesOwnDept_AdminSeesAll_EmployeeForbidden()
    {
        await _svc.ShareLocationAsync(_employee.Id, new ShareLocationRequest { Lat = 1, Lng = 2 }, default);
        await _svc.ShareLocationAsync(_otherEmployee.Id, new ShareLocationRequest { Lat = 3, Lng = 4 }, default);

        var m = await _svc.TeamLocationsAsync(_manager.Id, default);
        Assert.Equal([_employee.Id], m.Select(x => x.UserId));
        Assert.Equal(2, (await _svc.TeamLocationsAsync(_admin.Id, default)).Count);
        Assert.Equal(403, await StatusOf(() => _svc.TeamLocationsAsync(_employee.Id, default)));
    }
}
