using System.ComponentModel.DataAnnotations;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;

public class RegisterRequest
{
    [Required, StringLength(50, MinimumLength = 3), RegularExpression(@"^[A-Za-z0-9_.]+$", ErrorMessage = "Username chi gom chu, so, dau _ va .")]
    public string Username { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "Mat khau phai tu 6 ky tu")]
    public string Password { get; set; } = "";

    [Required, StringLength(100)]
    public string FullName { get; set; } = "";

    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>"email" hoặc "sms"</summary>
    [Required]
    public string VerifyChannel { get; set; } = "";
}

public class LoginRequest
{
    [Required] public string Username { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class VerifyOtpRequest
{
    [Required] public int? OtpSessionId { get; set; }
    [Required] public string Code { get; set; } = "";
}

public class ResendOtpRequest
{
    [Required] public int? OtpSessionId { get; set; }
}

public class GoogleLoginRequest
{
    [Required] public string IdToken { get; set; } = "";
}

public class RefreshRequest
{
    [Required] public string RefreshToken { get; set; } = "";
}

public class AddContactRequest
{
    /// <summary>"email" hoặc "sms"</summary>
    [Required] public string Type { get; set; } = "";
    [Required] public string Value { get; set; } = "";
}

/// <summary>Phiên OTP. Riêng đăng nhập của Admin bỏ qua OTP: trả luôn <c>Auth</c> (OtpSessionId = 0, Channel = "none").</summary>
public record OtpSessionResponse(int OtpSessionId, string Channel, string MaskedDestination, AuthResponse? Auth = null);

public record UserInfo(
    int Id, string Username, string FullName, string? Email, bool EmailVerified,
    string? PhoneNumber, bool PhoneVerified, string Role, int? PositionId, string? Position,
    int? DepartmentId, bool HasFace);

public record AuthResponse(
    string AccessToken, DateTime AccessExpiresAt, string RefreshToken, DateTime RefreshExpiresAt, UserInfo User);

/// <summary>Kết quả verify-otp: Register/Login có token; AddContact chỉ trả user đã cập nhật.</summary>
public record VerifyOtpResponse(string Purpose, AuthResponse? Auth, UserInfo User);
