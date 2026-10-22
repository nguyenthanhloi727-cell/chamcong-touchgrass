using Microsoft.EntityFrameworkCore;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Tests;

public class CrudTests
{
    private readonly AppDbContext _db;
    private readonly MasterDataService _master;
    private readonly EmployeeAdminService _emp;
    private readonly AttendanceCrudService _att;
    private readonly LeaveRequestService _leave;
    private readonly User _admin, _manager, _employee, _otherEmployee;
    private readonly Position _mgrPos, _empPos;
    private readonly Department _deptA, _deptB;

    public CrudTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _master = new MasterDataService(_db);
        _emp = new EmployeeAdminService(_db);
        _att = new AttendanceCrudService(_db);
        _leave = new LeaveRequestService(_db);

        _mgrPos = new Position { Name = "Quan ly Kho", RoleGroup = Role.Manager };
        _empPos = new Position { Name = "Ke toan", RoleGroup = Role.Employee };
        _db.Positions.AddRange(_mgrPos, _empPos);
        _deptA = new Department { Name = "A" };
        _deptB = new Department { Name = "B" };
        _db.Departments.AddRange(_deptA, _deptB);
        _db.SaveChanges();

        _admin = Add("admin", Role.Admin, null, null);
        _manager = Add("manager1", Role.Manager, _mgrPos.Id, _deptA.Id);
        _employee = Add("employee1", Role.Employee, _empPos.Id, _deptA.Id);
        _otherEmployee = Add("employee2", Role.Employee, _empPos.Id, _deptB.Id);
    }

    private User Add(string name, Role role, int? positionId, int? deptId)
    {
        var u = new User
        {
            Username = name, FullName = name, Email = name + "@x.com", Role = role,
            PositionId = positionId, DepartmentId = deptId, Status = UserStatus.Active,
        };
        _db.Users.Add(u);
        _db.SaveChanges();
        return u;
    }

    private static async Task<int> StatusOf(Func<Task> act)
    {
        try { await act(); return 200; }
        catch (AppException e) { return e.StatusCode; }
    }

    private Task<EmployeeDto> NewEmployee(string username = "newbie", string? email = "newbie@x.com", string? phone = null,
        Role role = Role.Employee, int? positionId = -1) =>
        _emp.CreateAsync(new CreateEmployeeRequest
        {
            Username = username, Password = "Temp@123", FullName = "Nhan Vien Moi", Email = email, Phone = phone,
            Role = role, PositionId = positionId == -1 ? _empPos.Id : positionId, DepartmentId = _deptA.Id,
        });

    // ---------- Phòng ban ----------

    [Fact]
    public async Task Department_FullCrud_AndSearch()
    {
        var d = await _master.CreateDepartmentAsync(new DepartmentRequest { Name = "Kho van", ManagerId = _manager.Id }, default);
        Assert.Equal("manager1", d.ManagerName);

        d = await _master.UpdateDepartmentAsync(d.Id, new DepartmentRequest { Name = "Kho van 2" }, default);
        Assert.Equal("Kho van 2", d.Name);
        Assert.Null(d.ManagerId);

        Assert.Single(await _master.ListDepartmentsAsync("van", default));
        Assert.Equal(3, (await _master.ListDepartmentsAsync(null, default)).Count);

        await _master.DeleteDepartmentAsync(d.Id, default);
        Assert.Equal(404, await StatusOf(() => _master.GetDepartmentAsync(d.Id, default)));
    }

    [Fact]
    public async Task Department_Validation()
    {
        Assert.Equal(409, await StatusOf(() => _master.CreateDepartmentAsync(new DepartmentRequest { Name = "A" }, default)));
        // nhân viên thường không làm trưởng phòng được
        Assert.Equal(400, await StatusOf(() => _master.CreateDepartmentAsync(
            new DepartmentRequest { Name = "C", ManagerId = _employee.Id }, default)));
        Assert.Equal(400, await StatusOf(() => _master.CreateDepartmentAsync(
            new DepartmentRequest { Name = "C", ManagerId = 9999 }, default)));
    }

    [Fact]
    public async Task Department_DeleteWithEmployees_409()
    {
        Assert.Equal(409, await StatusOf(() => _master.DeleteDepartmentAsync(_deptA.Id, default)));
    }

    // ---------- Chức vụ ----------

    [Fact]
    public async Task Position_Crud_Rules()
    {
        var p = await _master.CreatePositionAsync(new PositionRequest { Name = "Bao ve", RoleGroup = Role.Employee }, default);
        Assert.Equal(400, await StatusOf(() => _master.CreatePositionAsync(
            new PositionRequest { Name = "X", RoleGroup = Role.Admin }, default)));
        Assert.Equal(409, await StatusOf(() => _master.CreatePositionAsync(
            new PositionRequest { Name = "Bao ve", RoleGroup = Role.Employee }, default)));

        p = await _master.UpdatePositionAsync(p.Id, new PositionRequest { Name = "Bao ve dem", RoleGroup = Role.Employee }, default);
        Assert.Equal("Bao ve dem", p.Name);
        Assert.Single(await _master.ListPositionsAsync("dem", null, default));

        await _master.DeletePositionAsync(p.Id, default);
        Assert.Equal(404, await StatusOf(() => _master.GetPositionAsync(p.Id, default)));
    }

    [Fact]
    public async Task Position_InUse_CannotDeleteOrChangeGroup()
    {
        Assert.Equal(409, await StatusOf(() => _master.DeletePositionAsync(_empPos.Id, default)));
        Assert.Equal(409, await StatusOf(() => _master.UpdatePositionAsync(
            _empPos.Id, new PositionRequest { Name = "Ke toan", RoleGroup = Role.Manager }, default)));
    }

    // ---------- Ca làm ----------

    [Fact]
    public async Task Shift_StartMustBeBeforeEnd()
    {
        Assert.Equal(400, await StatusOf(() => _master.CreateShiftAsync(new ShiftRequest
            { Name = "Loi", StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(8, 0) }, default)));
        Assert.Equal(400, await StatusOf(() => _master.CreateShiftAsync(new ShiftRequest
            { Name = "Loi", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(8, 0) }, default)));

        var s = await _master.CreateShiftAsync(new ShiftRequest
            { Name = "Sang", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0) }, default);
        s = await _master.UpdateShiftAsync(s.Id, new ShiftRequest
            { Name = "Sang", StartTime = new TimeOnly(7, 30), EndTime = new TimeOnly(12, 0) }, default);
        Assert.Equal(new TimeOnly(7, 30), s.StartTime);
        Assert.Equal(400, await StatusOf(() => _master.UpdateShiftAsync(s.Id, new ShiftRequest
            { Name = "Sang", StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(12, 0) }, default)));
        Assert.Single(await _master.ListShiftsAsync("Sang", default));

        await _master.DeleteShiftAsync(s.Id, default);
        Assert.Equal(404, await StatusOf(() => _master.GetShiftAsync(s.Id, default)));
    }

    [Fact]
    public async Task Shift_WithAttendance_CannotDelete()
    {
        var s = await _master.CreateShiftAsync(new ShiftRequest
            { Name = "Sang", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0) }, default);
        await _att.CreateAsync(_admin.Id, new CreateAttendanceRequest
            { UserId = _employee.Id, ShiftId = s.Id, CheckInAt = DateTime.UtcNow }, default);
        Assert.Equal(409, await StatusOf(() => _master.DeleteShiftAsync(s.Id, default)));
    }

    // ---------- Địa điểm ----------

    [Fact]
    public async Task Location_Crud()
    {
        var l = await _master.CreateLocationAsync(new LocationRequest { Name = "Van phong", Lat = 10.77, Lng = 106.69, RadiusMeters = 100 }, default);
        l = await _master.UpdateLocationAsync(l.Id, new LocationRequest { Name = "Van phong 2", Lat = 10.78, Lng = 106.7, RadiusMeters = 150 }, default);
        Assert.Equal(150, l.RadiusMeters);
        Assert.Single(await _master.ListLocationsAsync("phong", default));
        await _master.DeleteLocationAsync(l.Id, default);
        Assert.Equal(404, await StatusOf(() => _master.GetLocationAsync(l.Id, default)));
    }

    // ---------- Nhân viên ----------

    [Fact]
    public async Task Employee_Create_HashesPassword_AndHidesIt()
    {
        var e = await NewEmployee(phone: "0912345678");
        Assert.Equal("+84912345678", e.PhoneNumber);
        Assert.Equal("Active", e.Status);

        var u = await _db.Users.FirstAsync(x => x.Id == e.Id);
        Assert.NotEqual("Temp@123", u.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Temp@123", u.PasswordHash));
        Assert.True(u.EmailVerified && u.PhoneVerified);

        // DTO không có trường mật khẩu
        Assert.DoesNotContain(typeof(EmployeeDto).GetProperties(), p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Employee_Create_Validation()
    {
        Assert.Equal(400, await StatusOf(() => NewEmployee(email: null, phone: null)));
        Assert.Equal(400, await StatusOf(() => NewEmployee(email: "khong-hop-le")));
        Assert.Equal(400, await StatusOf(() => NewEmployee(role: Role.Manager)));          // chức vụ Employee gán cho Manager
        Assert.Equal(400, await StatusOf(() => NewEmployee(role: Role.Admin)));            // Admin không có chức vụ
        Assert.Equal(400, await StatusOf(() => NewEmployee(positionId: 9999)));
        Assert.Equal(409, await StatusOf(() => NewEmployee(username: "employee1", email: "other@x.com")));
        Assert.Equal(409, await StatusOf(() => NewEmployee(email: "employee1@x.com")));

        await NewEmployee(phone: "0912345678");
        Assert.Equal(409, await StatusOf(() => NewEmployee(username: "n2", email: "n2@x.com", phone: "0912345678")));
    }

    [Fact]
    public async Task Employee_Update_AssignDepartment_LockAndResetPassword()
    {
        var e = await _emp.UpdateAsync(_admin.Id, _manager.Id, new UpdateEmployeeRequest
            { FullName = "Quan Ly", Email = "manager1@x.com", DepartmentId = _deptB.Id, NewPassword = "Moi@1234" });
        Assert.Equal(_deptB.Id, e.DepartmentId);
        Assert.True(BCrypt.Net.BCrypt.Verify("Moi@1234", (await _db.Users.FindAsync(_manager.Id))!.PasswordHash));

        e = await _emp.UpdateAsync(_admin.Id, _employee.Id, new UpdateEmployeeRequest
            { FullName = "NV", Email = "employee1@x.com", DepartmentId = _deptA.Id, Status = UserStatus.Locked });
        Assert.Equal("Locked", e.Status);

        Assert.Equal(400, await StatusOf(() => _emp.UpdateAsync(_admin.Id, _employee.Id, new UpdateEmployeeRequest
            { FullName = "NV", Email = "employee1@x.com", DepartmentId = 9999 })));
        Assert.Equal(404, await StatusOf(() => _emp.UpdateAsync(_admin.Id, 9999, new UpdateEmployeeRequest
            { FullName = "NV", Email = "a@x.com" })));
    }

    [Fact]
    public async Task Employee_AdminProtection()
    {
        // tự khóa mình
        Assert.Equal(403, await StatusOf(() => _emp.UpdateAsync(_admin.Id, _admin.Id, new UpdateEmployeeRequest
            { FullName = "a", Email = "admin@x.com", Status = UserStatus.Locked })));
        // tự xóa mình
        Assert.Equal(403, await StatusOf(() => _emp.DeleteAsync(_admin.Id, _admin.Id)));
        // admin cuối cùng bị người khác (manager giả làm actor) khóa / xóa
        Assert.Equal(409, await StatusOf(() => _emp.UpdateAsync(_manager.Id, _admin.Id, new UpdateEmployeeRequest
            { FullName = "a", Email = "admin@x.com", Status = UserStatus.Locked })));
        Assert.Equal(409, await StatusOf(() => _emp.DeleteAsync(_manager.Id, _admin.Id)));
    }

    [Fact]
    public async Task Employee_Delete_Referenced_409_ElseOk()
    {
        await _att.CreateAsync(_admin.Id, new CreateAttendanceRequest { UserId = _employee.Id, CheckInAt = DateTime.UtcNow }, default);
        Assert.Equal(409, await StatusOf(() => _emp.DeleteAsync(_admin.Id, _employee.Id)));

        _deptB.ManagerId = _manager.Id;
        await _db.SaveChangesAsync();
        Assert.Equal(409, await StatusOf(() => _emp.DeleteAsync(_admin.Id, _manager.Id)));

        await _emp.DeleteAsync(_admin.Id, _otherEmployee.Id);
        Assert.Null(await _db.Users.FindAsync(_otherEmployee.Id));
        Assert.Equal(404, await StatusOf(() => _emp.DeleteAsync(_admin.Id, _otherEmployee.Id)));
    }

    // ---------- Chấm công ----------

    [Fact]
    public async Task Attendance_Visibility_ByRole()
    {
        await _att.CreateAsync(_admin.Id, new CreateAttendanceRequest { UserId = _employee.Id, CheckInAt = DateTime.UtcNow }, default);
        await _att.CreateAsync(_admin.Id, new CreateAttendanceRequest { UserId = _otherEmployee.Id, CheckInAt = DateTime.UtcNow }, default);

        Assert.Equal(2, (await _att.ListAsync(_admin.Id, null, null, null, null, null, default)).Count);
        var mgr = await _att.ListAsync(_manager.Id, null, null, null, null, null, default);
        Assert.Equal(["employee1"], mgr.Select(a => a.Username));
        var own = await _att.ListAsync(_employee.Id, null, null, null, null, null, default);
        Assert.Equal(["employee1"], own.Select(a => a.Username));

        var otherId = (await _att.ListAsync(_admin.Id, "employee2", null, null, null, null, default)).Single().Id;
        Assert.Equal(404, await StatusOf(() => _att.GetAsync(_manager.Id, otherId, default)));
        Assert.Equal(404, await StatusOf(() => _att.GetAsync(_employee.Id, otherId, default)));
    }

    [Fact]
    public async Task Attendance_Update_StatusAndScope()
    {
        var a = await _att.CreateAsync(_admin.Id, new CreateAttendanceRequest
            { UserId = _employee.Id, CheckInAt = new DateTime(2026, 10, 22, 8, 0, 0), Status = AttendanceStatus.PendingReview }, default);

        var u = await _att.UpdateAsync(_manager.Id, a.Id, new UpdateAttendanceRequest { Status = AttendanceStatus.OnTime }, default);
        Assert.Equal("OnTime", u.Status);

        // nhân viên không sửa được (scope chỉ thấy của mình, nhưng không có quyền quản lý)
        Assert.Equal(403, await StatusOf(() => _att.UpdateAsync(_employee.Id, a.Id,
            new UpdateAttendanceRequest { Status = AttendanceStatus.OnTime }, default)));
        // giờ ra trước giờ vào
        Assert.Equal(400, await StatusOf(() => _att.UpdateAsync(_admin.Id, a.Id, new UpdateAttendanceRequest
            { Status = AttendanceStatus.OnTime, CheckOutAt = new DateTime(2026, 10, 22, 7, 0, 0) }, default)));
        // manager không nhập hộ nhân viên phòng khác
        Assert.Equal(403, await StatusOf(() => _att.CreateAsync(_manager.Id,
            new CreateAttendanceRequest { UserId = _otherEmployee.Id, CheckInAt = DateTime.UtcNow }, default)));
    }

    [Fact]
    public async Task Attendance_Filters_AndDelete()
    {
        await _att.CreateAsync(_admin.Id, new CreateAttendanceRequest
            { UserId = _employee.Id, CheckInAt = new DateTime(2026, 10, 21, 8, 0, 0), Status = AttendanceStatus.Late }, default);
        await _att.CreateAsync(_admin.Id, new CreateAttendanceRequest
            { UserId = _employee.Id, CheckInAt = new DateTime(2026, 10, 22, 8, 0, 0), Status = AttendanceStatus.OnTime }, default);

        Assert.Single(await _att.ListAsync(_admin.Id, null, null, new DateOnly(2026, 10, 22), new DateOnly(2026, 10, 22), null, default));
        Assert.Single(await _att.ListAsync(_admin.Id, null, null, null, null, AttendanceStatus.Late, default));

        var id = (await _att.ListAsync(_admin.Id, null, null, null, null, null, default))[0].Id;
        await _att.DeleteAsync(id, default);
        Assert.Equal(404, await StatusOf(() => _att.GetAsync(_admin.Id, id, default)));
    }

    // ---------- Đơn nghỉ phép ----------

    private static LeaveRequestRequest Leave(int day = 25) =>
        new() { FromDate = new DateOnly(2026, 10, day), ToDate = new DateOnly(2026, 10, day + 1), Reason = "Viec rieng" };

    [Fact]
    public async Task Leave_CreateUpdateDelete_OwnerOnlyWhilePending()
    {
        var l = await _leave.CreateAsync(_employee.Id, Leave(), default);
        Assert.Equal("Pending", l.Status);

        l = await _leave.UpdateAsync(_employee.Id, l.Id, Leave(27), default);
        Assert.Equal(new DateOnly(2026, 10, 27), l.FromDate);
        Assert.Equal(404, await StatusOf(() => _leave.UpdateAsync(_otherEmployee.Id, l.Id, Leave(), default)));
        Assert.Equal(400, await StatusOf(() => _leave.CreateAsync(_employee.Id,
            new LeaveRequestRequest { FromDate = new DateOnly(2026, 10, 5), ToDate = new DateOnly(2026, 10, 1), Reason = "x" }, default)));

        await _leave.DeleteAsync(_employee.Id, l.Id, default);
        Assert.Equal(404, await StatusOf(() => _leave.GetAsync(_employee.Id, l.Id, default)));
    }

    [Fact]
    public async Task Leave_ApproveReject_Rules()
    {
        var l1 = await _leave.CreateAsync(_employee.Id, Leave(), default);
        var l2 = await _leave.CreateAsync(_employee.Id, Leave(28), default);

        var a = await _leave.ApproveAsync(_manager.Id, l1.Id, default);
        Assert.Equal("Approved", a.Status);
        Assert.Equal(_manager.Id, a.ApprovedBy);
        var r = await _leave.RejectAsync(_admin.Id, l2.Id, default);
        Assert.Equal("Rejected", r.Status);

        // đã xử lý rồi → 409; sửa/xóa đơn đã xử lý → 409
        Assert.Equal(409, await StatusOf(() => _leave.RejectAsync(_manager.Id, l1.Id, default)));
        Assert.Equal(409, await StatusOf(() => _leave.UpdateAsync(_employee.Id, l1.Id, Leave(), default)));
        Assert.Equal(409, await StatusOf(() => _leave.DeleteAsync(_employee.Id, l1.Id, default)));
    }

    [Fact]
    public async Task Leave_ApproveScope()
    {
        var other = await _leave.CreateAsync(_otherEmployee.Id, Leave(), default);
        // manager phòng A không thấy / không duyệt đơn phòng B
        Assert.Equal(404, await StatusOf(() => _leave.ApproveAsync(_manager.Id, other.Id, default)));
        // nhân viên không duyệt
        var mine = await _leave.CreateAsync(_employee.Id, Leave(), default);
        Assert.Equal(403, await StatusOf(() => _leave.ApproveAsync(_employee.Id, mine.Id, default)));
        // manager không tự duyệt đơn của mình
        var own = await _leave.CreateAsync(_manager.Id, Leave(), default);
        Assert.Equal(403, await StatusOf(() => _leave.ApproveAsync(_manager.Id, own.Id, default)));
        Assert.Equal("Approved", (await _leave.ApproveAsync(_admin.Id, own.Id, default)).Status);

        // danh sách theo vai trò
        Assert.Equal(2, (await _leave.ListAsync(_manager.Id, null, null, default)).Count);   // đơn của nv1 + của chính mình
        Assert.Single(await _leave.ListAsync(_employee.Id, null, null, default));
        Assert.Equal(3, (await _leave.ListAsync(_admin.Id, null, null, default)).Count);
        Assert.Single(await _leave.ListAsync(_admin.Id, null, "approved", default));
        Assert.Equal(400, await StatusOf(() => _leave.ListAsync(_admin.Id, null, "bogus", default)));
    }
}
