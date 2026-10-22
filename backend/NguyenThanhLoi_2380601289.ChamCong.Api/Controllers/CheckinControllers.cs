using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Checkin;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

[Authorize(Roles = "Admin,Manager")]
[Route("api/qr")]
public class QrController(QrService qr) : AppControllerBase
{
    /// <summary>QR hiện tại (đổi mỗi 30s). Màn hình lễ tân/quản lý hiển thị mã này.</summary>
    [HttpGet("current")]
    public QrDto Current() { var (t, e) = qr.Current(); return new QrDto(t, e); }
}

[Authorize]
public class AttendanceActionsController(CheckinService svc) : AppControllerBase
{
    [HttpPost("api/attendance/check-in")]
    public Task<CheckInResult> CheckIn(CheckInRequest req, CancellationToken ct) => svc.CheckInAsync(UserId, req, ct);

    [HttpPost("api/attendance/check-out")]
    public Task<AttendanceDto> CheckOut(CancellationToken ct) => svc.CheckOutAsync(UserId, ct);

    [HttpPost("api/attendances/{id:int}/review"), Authorize(Roles = "Admin,Manager")]
    public Task<AttendanceDto> Review(int id, ReviewRequest req, CancellationToken ct) =>
        svc.ReviewAsync(UserId, id, req.Approve ?? throw AppException.BadRequest("Thieu approve"), ct);

    [HttpGet("api/reports/monthly"), Authorize(Roles = "Admin,Manager")]
    public Task<List<MonthlyReportRow>> Monthly([FromQuery] string? month, [FromQuery] int? departmentId, CancellationToken ct) =>
        svc.MonthlyReportAsync(UserId, month, departmentId, ct);

    [HttpPost("api/location/share")]
    public async Task<IActionResult> ShareLocation(ShareLocationRequest req, CancellationToken ct)
    {
        await svc.ShareLocationAsync(UserId, req, ct);
        return NoContent();
    }

    [HttpGet("api/location/team"), Authorize(Roles = "Admin,Manager")]
    public Task<List<TeamLocationDto>> Team(CancellationToken ct) => svc.TeamLocationsAsync(UserId, ct);
}
