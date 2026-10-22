using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Auth;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Đăng ký (Email và/hoặc SĐT, ít nhất 1) → gửi OTP qua kênh đã chọn.</summary>
    [HttpPost("register")]
    public async Task<OtpSessionResponse> Register(RegisterRequest req, CancellationToken ct) =>
        await auth.RegisterAsync(req, ct);

    /// <summary>Xác nhận OTP cho đăng ký / đăng nhập / bổ sung liên hệ.</summary>
    [HttpPost("verify-otp")]
    public async Task<VerifyOtpResponse> VerifyOtp(VerifyOtpRequest req, CancellationToken ct) =>
        await auth.VerifyOtpAsync(req, ct);

    /// <summary>Username + mật khẩu → gửi OTP qua SMS (nếu SĐT đã xác nhận) hoặc Email. Admin: trả token luôn (field auth).</summary>
    [HttpPost("login")]
    public async Task<OtpSessionResponse> Login(LoginRequest req, CancellationToken ct) =>
        await auth.LoginAsync(req, ct);

    [HttpPost("resend-otp")]
    public async Task<OtpSessionResponse> ResendOtp(ResendOtpRequest req, CancellationToken ct) =>
        await auth.ResendOtpAsync(req, ct);

    [HttpPost("google")]
    public async Task<AuthResponse> Google(GoogleLoginRequest req, CancellationToken ct) =>
        await auth.GoogleAsync(req, ct);

    [HttpPost("refresh")]
    public async Task<AuthResponse> Refresh(RefreshRequest req, CancellationToken ct) =>
        await auth.RefreshAsync(req, ct);
}
