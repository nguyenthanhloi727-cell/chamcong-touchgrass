using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

[Authorize]
[Route("api/departments")]
public class DepartmentsController(MasterDataService svc) : AppControllerBase
{
    [HttpGet]
    public Task<List<DepartmentDto>> List([FromQuery] string? q, CancellationToken ct) => svc.ListDepartmentsAsync(q, ct);

    [HttpGet("{id:int}")]
    public Task<DepartmentDto> Get(int id, CancellationToken ct) => svc.GetDepartmentAsync(id, ct);

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<DepartmentDto>> Create(DepartmentRequest req, CancellationToken ct)
    {
        var d = await svc.CreateDepartmentAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = d.Id }, d);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public Task<DepartmentDto> Update(int id, DepartmentRequest req, CancellationToken ct) => svc.UpdateDepartmentAsync(id, req, ct);

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await svc.DeleteDepartmentAsync(id, ct);
        return NoContent();
    }
}
