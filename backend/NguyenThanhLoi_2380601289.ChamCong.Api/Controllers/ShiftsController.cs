using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

[Authorize]
[Route("api/shifts")]
public class ShiftsController(MasterDataService svc) : AppControllerBase
{
    [HttpGet]
    public Task<List<ShiftDto>> List([FromQuery] string? q, CancellationToken ct) => svc.ListShiftsAsync(q, ct);

    [HttpGet("{id:int}")]
    public Task<ShiftDto> Get(int id, CancellationToken ct) => svc.GetShiftAsync(id, ct);

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<ShiftDto>> Create(ShiftRequest req, CancellationToken ct)
    {
        var s = await svc.CreateShiftAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = s.Id }, s);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public Task<ShiftDto> Update(int id, ShiftRequest req, CancellationToken ct) => svc.UpdateShiftAsync(id, req, ct);

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await svc.DeleteShiftAsync(id, ct);
        return NoContent();
    }
}
