using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Face;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

[Route("api/face")]
public class FaceController(FaceService face) : AppControllerBase
{
    [Authorize, HttpPost("enroll")]
    public async Task<IActionResult> Enroll(FaceEnrollRequest req, CancellationToken ct)
    {
        await face.EnrollAsync(UserId, req.Embedding, ct);
        return NoContent();
    }

    [Authorize, HttpPost("verify")]
    public Task<FaceVerifyResponse> Verify(FaceVerifyRequest req, CancellationToken ct) =>
        face.VerifyAsync(UserId, req.Embedding, ct);

    [AllowAnonymous, HttpPost("login")]
    public Task<AuthResponse> Login(FaceLoginRequest req, CancellationToken ct) => face.LoginAsync(req, ct);
}
