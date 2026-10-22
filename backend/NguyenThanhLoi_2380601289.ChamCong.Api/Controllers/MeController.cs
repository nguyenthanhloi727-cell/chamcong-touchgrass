using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Auth;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class MeController(AuthService auth) : ControllerBase
{
    private int UserId => int.TryParse(User.FindFirst("sub")?.Value, out var id)
        ? id
        : throw AppException.Unauthorized("Token khong hop le");

    [HttpGet]
    public async Task<UserInfo> Get(CancellationToken ct) => await auth.GetMeAsync(UserId, ct);

    /// <summary>Thêm / xác nhận Email hoặc SĐT; hoàn tất bằng POST /api/auth/verify-otp.</summary>
    [HttpPost("contact")]
    public async Task<OtpSessionResponse> AddContact(AddContactRequest req, CancellationToken ct) =>
        await auth.AddContactAsync(UserId, req, ct);
}
