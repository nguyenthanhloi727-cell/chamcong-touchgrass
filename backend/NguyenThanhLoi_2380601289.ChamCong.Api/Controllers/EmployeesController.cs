using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Rbac;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

[Route("api/employees")]
public class EmployeesController(RbacService rbac, EmployeeAdminService admin) : AppControllerBase
{
    /// <summary>Admin: tất cả. Manager: chỉ nhân viên cùng phòng ban. ?q= tìm theo username/họ tên.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public Task<List<EmployeeDto>> List([FromQuery] string? q, CancellationToken ct) =>
        rbac.ListEmployeesAsync(UserId, q, ct);

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public Task<EmployeeDto> Get(int id, CancellationToken ct) => rbac.GetEmployeeAsync(UserId, id, ct);

    /// <summary>Admin tạo nhân viên với mật khẩu tạm.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeRequest req, CancellationToken ct)
    {
        var e = await admin.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = e.Id }, e);
    }

    /// <summary>Sửa họ tên, liên hệ, phòng ban, khóa/mở, đặt lại mật khẩu (Role/Chức vụ đổi ở PUT {id}/role).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public Task<EmployeeDto> Update(int id, UpdateEmployeeRequest req, CancellationToken ct) =>
        admin.UpdateAsync(UserId, id, req, ct);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await admin.DeleteAsync(UserId, id, ct);
        return NoContent();
    }

    /// <summary>Đổi Role + Chức vụ (chỉ Admin).</summary>
    [HttpPut("{id:int}/role")]
    [Authorize(Roles = "Admin")]
    public Task<EmployeeDto> ChangeRole(int id, ChangeRoleRequest req, CancellationToken ct) =>
        rbac.ChangeRoleAsync(UserId, id, req, ct);
}
