using Microsoft.EntityFrameworkCore;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Rbac;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Tests;

public class RbacTests
{
    private readonly AppDbContext _db;
    private readonly RbacService _rbac;
    private readonly User _admin, _admin2, _manager, _employee, _otherDeptEmployee;
    private readonly Position _mgrPos, _empPos;

    public RbacTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _rbac = new RbacService(_db);

        _mgrPos = new Position { Name = "Quan ly Kho", RoleGroup = Role.Manager };
        _empPos = new Position { Name = "Ke toan", RoleGroup = Role.Employee };
        _db.Positions.AddRange(_mgrPos, _empPos);
        _db.Departments.AddRange(new Department { Name = "A" }, new Department { Name = "B" });
        _db.SaveChanges();

        _admin = Add("admin", Role.Admin, null, null);
        _manager = Add("manager1", Role.Manager, _mgrPos.Id, 1);
        _employee = Add("employee1", Role.Employee, _empPos.Id, 1);
        _otherDeptEmployee = Add("employee2", Role.Employee, _empPos.Id, 2);
        _admin2 = Add("admin2", Role.Admin, null, null);
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

    [Fact]
    public async Task ChangeRole_PositionMustMatchRoleGroup()
    {
        // gán Kế toán (Employee) cho Manager -> 400
        Assert.Equal(400, await StatusOf(() => _rbac.ChangeRoleAsync(_admin.Id, _employee.Id,
            new ChangeRoleRequest { Role = Role.Manager, PositionId = _empPos.Id })));
        Assert.Equal(400, await StatusOf(() => _rbac.ChangeRoleAsync(_admin.Id, _employee.Id,
            new ChangeRoleRequest { Role = Role.Employee, PositionId = _mgrPos.Id })));
        Assert.Equal(400, await StatusOf(() => _rbac.ChangeRoleAsync(_admin.Id, _employee.Id,
            new ChangeRoleRequest { Role = Role.Manager, PositionId = null })));
        Assert.Equal(400, await StatusOf(() => _rbac.ChangeRoleAsync(_admin.Id, _employee.Id,
            new ChangeRoleRequest { Role = Role.Manager, PositionId = 999 })));
    }

    [Fact]
    public async Task ChangeRole_PromoteEmployeeToManager_Works()
    {
        var r = await _rbac.ChangeRoleAsync(_admin.Id, _employee.Id,
            new ChangeRoleRequest { Role = Role.Manager, PositionId = _mgrPos.Id });
        Assert.Equal("Manager", r.Role);
        Assert.Equal(_mgrPos.Id, r.PositionId);
    }

    [Fact]
    public async Task ChangeRole_AdminHasNoPosition()
    {
        Assert.Equal(400, await StatusOf(() => _rbac.ChangeRoleAsync(_admin.Id, _employee.Id,
            new ChangeRoleRequest { Role = Role.Admin, PositionId = _empPos.Id })));

        var r = await _rbac.ChangeRoleAsync(_admin.Id, _employee.Id, new ChangeRoleRequest { Role = Role.Admin });
        Assert.Equal("Admin", r.Role);
        Assert.Null(r.PositionId);
    }

    [Fact]
    public async Task ChangeRole_CannotDemoteSelf()
    {
        Assert.Equal(403, await StatusOf(() => _rbac.ChangeRoleAsync(_admin.Id, _admin.Id,
            new ChangeRoleRequest { Role = Role.Employee, PositionId = _empPos.Id })));
    }

    [Fact]
    public async Task ChangeRole_CannotSelfPromoteToAdmin()
    {
        Assert.Equal(403, await StatusOf(() => _rbac.ChangeRoleAsync(_manager.Id, _manager.Id,
            new ChangeRoleRequest { Role = Role.Admin })));
    }

    [Fact]
    public async Task ChangeRole_CannotDemoteLastAdmin()
    {
        // admin2 hạ admin (còn 2 admin) -> được
        await _rbac.ChangeRoleAsync(_admin2.Id, _admin.Id,
            new ChangeRoleRequest { Role = Role.Employee, PositionId = _empPos.Id });

        // chỉ còn admin2, người khác (manager giả làm actor) cố hạ -> 409
        Assert.Equal(409, await StatusOf(() => _rbac.ChangeRoleAsync(_manager.Id, _admin2.Id,
            new ChangeRoleRequest { Role = Role.Employee, PositionId = _empPos.Id })));
    }

    [Fact]
    public async Task ChangeRole_UnknownUser_404()
    {
        Assert.Equal(404, await StatusOf(() => _rbac.ChangeRoleAsync(_admin.Id, 9999,
            new ChangeRoleRequest { Role = Role.Employee, PositionId = _empPos.Id })));
    }

    [Fact]
    public async Task List_ManagerSeesOnlyOwnDepartment()
    {
        var list = await _rbac.ListEmployeesAsync(_manager.Id, null);
        Assert.Equal(new[] { "employee1", "manager1" }, list.Select(x => x.Username).OrderBy(x => x));

        Assert.Equal(404, await StatusOf(() => _rbac.GetEmployeeAsync(_manager.Id, _otherDeptEmployee.Id)));
        Assert.Equal(_employee.Id, (await _rbac.GetEmployeeAsync(_manager.Id, _employee.Id)).Id);
    }

    [Fact]
    public async Task List_AdminSeesAll_EmployeeForbidden()
    {
        Assert.Equal(5, (await _rbac.ListEmployeesAsync(_admin.Id, null)).Count);
        Assert.Equal(403, await StatusOf(() => _rbac.ListEmployeesAsync(_employee.Id, null)));
    }

    [Fact]
    public async Task Positions_FilterByRoleGroup()
    {
        Assert.Single(await _rbac.ListPositionsAsync(Role.Manager));
        Assert.Equal(2, (await _rbac.ListPositionsAsync(null)).Count);
    }
}
