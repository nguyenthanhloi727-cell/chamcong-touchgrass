using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

[Authorize]
[Route("api/leave-requests")]
public class LeaveRequestsController(LeaveRequestService svc) : AppControllerBase
{
    /// <summary>Admin: tất cả; Manager: phòng mình; Employee: của mình. ?status=Pending|Approved|Rejected.</summary>
    [HttpGet]
    public Task<List<LeaveRequestDto>> List([FromQuery] string? q, [FromQuery] string? status, CancellationToken ct) =>
        svc.ListAsync(UserId, q, status, ct);

    [HttpGet("{id:int}")]
    public Task<LeaveRequestDto> Get(int id, CancellationToken ct) => svc.GetAsync(UserId, id, ct);

    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> Create(LeaveRequestRequest req, CancellationToken ct)
    {
        var l = await svc.CreateAsync(UserId, req, ct);
        return CreatedAtAction(nameof(Get), new { id = l.Id }, l);
    }

    [HttpPut("{id:int}")]
    public Task<LeaveRequestDto> Update(int id, LeaveRequestRequest req, CancellationToken ct) =>
        svc.UpdateAsync(UserId, id, req, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await svc.DeleteAsync(UserId, id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/approve"), Authorize(Roles = "Admin,Manager")]
    public Task<LeaveRequestDto> Approve(int id, CancellationToken ct) => svc.ApproveAsync(UserId, id, ct);

    [HttpPost("{id:int}/reject"), Authorize(Roles = "Admin,Manager")]
    public Task<LeaveRequestDto> Reject(int id, CancellationToken ct) => svc.RejectAsync(UserId, id, ct);
}
