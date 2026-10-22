using Microsoft.AspNetCore.Mvc;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}
