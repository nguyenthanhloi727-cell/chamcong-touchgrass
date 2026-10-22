using Microsoft.AspNetCore.Mvc;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

[ApiController]
public abstract class AppControllerBase : ControllerBase
{
    protected int UserId => int.TryParse(User.FindFirst("sub")?.Value, out var id)
        ? id
        : throw AppException.Unauthorized("Token khong hop le");
}
