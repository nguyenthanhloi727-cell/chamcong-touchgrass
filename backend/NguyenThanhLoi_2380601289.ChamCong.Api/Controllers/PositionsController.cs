using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Rbac;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/positions")]
public class PositionsController(RbacService rbac) : ControllerBase
{
    /// <summary>Cho dropdown ô 2: ?roleGroup=Manager|Employee.</summary>
    [HttpGet]
    public Task<List<PositionDto>> List([FromQuery] string? roleGroup, CancellationToken ct)
    {
        Role? group = null;
        if (!string.IsNullOrWhiteSpace(roleGroup))
        {
            if (!Enum.TryParse<Role>(roleGroup, true, out var r) || r == Role.Admin)
                throw AppException.BadRequest("roleGroup phai la Manager hoac Employee");
            group = r;
        }
        return rbac.ListPositionsAsync(group, ct);
    }
}
