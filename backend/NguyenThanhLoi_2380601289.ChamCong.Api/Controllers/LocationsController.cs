using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

[Authorize]
[Route("api/locations")]
public class LocationsController(MasterDataService svc) : AppControllerBase
{
    [HttpGet]
    public Task<List<LocationDto>> List([FromQuery] string? q, CancellationToken ct) => svc.ListLocationsAsync(q, ct);

    [HttpGet("{id:int}")]
    public Task<LocationDto> Get(int id, CancellationToken ct) => svc.GetLocationAsync(id, ct);

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<LocationDto>> Create(LocationRequest req, CancellationToken ct)
    {
        var l = await svc.CreateLocationAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = l.Id }, l);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public Task<LocationDto> Update(int id, LocationRequest req, CancellationToken ct) => svc.UpdateLocationAsync(id, req, ct);

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await svc.DeleteLocationAsync(id, ct);
        return NoContent();
    }
}
