using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

/// <summary>Quản lý bản ghi chấm công (check-in/out thật nằm ở /api/attendance, Bước 8).</summary>
[Authorize]
[Route("api/attendances")]
public class AttendancesController(AttendanceCrudService svc) : AppControllerBase
{
    /// <summary>Admin: tất cả; Manager: phòng mình; Employee: của mình. Lọc: q, userId, from, to (yyyy-MM-dd), status.</summary>
    [HttpGet]
    public Task<List<AttendanceDto>> List([FromQuery] string? q, [FromQuery] int? userId, [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to, [FromQuery] AttendanceStatus? status, CancellationToken ct) =>
        svc.ListAsync(UserId, q, userId, from, to, status, ct);

    [HttpGet("{id:int}")]
    public Task<AttendanceDto> Get(int id, CancellationToken ct) => svc.GetAsync(UserId, id, ct);

    [HttpPost, Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<AttendanceDto>> Create(CreateAttendanceRequest req, CancellationToken ct)
    {
        var a = await svc.CreateAsync(UserId, req, ct);
        return CreatedAtAction(nameof(Get), new { id = a.Id }, a);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin,Manager")]
    public Task<AttendanceDto> Update(int id, UpdateAttendanceRequest req, CancellationToken ct) =>
        svc.UpdateAsync(UserId, id, req, ct);

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
