using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

[Authorize]
[Route("api/positions")]
public class PositionsController(MasterDataService svc) : AppControllerBase
{
    /// <summary>Cho dropdown ô 2: ?roleGroup=Manager|Employee; ?q= tìm theo tên.</summary>
    [HttpGet]
    public Task<List<PositionDto>> List([FromQuery] string? roleGroup, [FromQuery] string? q, CancellationToken ct)
    {
        Role? group = null;
        if (!string.IsNullOrWhiteSpace(roleGroup))
        {
            if (!Enum.TryParse<Role>(roleGroup, true, out var r) || r == Role.Admin)
                throw AppException.BadRequest("roleGroup phai la Manager hoac Employee");
            group = r;
        }
        return svc.ListPositionsAsync(q, group, ct);
    }

    [HttpGet("{id:int}")]
    public Task<PositionDto> Get(int id, CancellationToken ct) => svc.GetPositionAsync(id, ct);

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<PositionDto>> Create(PositionRequest req, CancellationToken ct)
    {
        var p = await svc.CreatePositionAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = p.Id }, p);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public Task<PositionDto> Update(int id, PositionRequest req, CancellationToken ct) => svc.UpdatePositionAsync(id, req, ct);

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await svc.DeletePositionAsync(id, ct);
        return NoContent();
    }
}
