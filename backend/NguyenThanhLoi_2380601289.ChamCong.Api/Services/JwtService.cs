using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Options;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services;

public record TokenPair(string AccessToken, DateTime AccessExpiresAt, string RefreshToken, DateTime RefreshExpiresAt);

public class JwtService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _o = options.Value;

    public static SymmetricSecurityKey CreateKey(string key) => new(Encoding.UTF8.GetBytes(key));

    public TokenPair CreateTokens(User user, string? positionName)
    {
        var now = DateTime.UtcNow;
        var accessExp = now.AddMinutes(_o.AccessMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("role", user.Role.ToString()), // khớp RoleClaimType = "role" ở Program.cs
            new("position", positionName ?? ""),
        };
        var creds = new SigningCredentials(CreateKey(_o.Key), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_o.Issuer, _o.Audience, claims, now, accessExp, creds);
        var access = new JwtSecurityTokenHandler().WriteToken(jwt);

        var refreshExp = now.AddDays(_o.RefreshDays);
        var refreshClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Convert.ToHexString(RandomNumberGenerator.GetBytes(16))),
            new("typ", "refresh"),
        };
        // audience riêng → refresh token không dùng được làm access token
        var refresh = new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(_o.Issuer, RefreshAudience, refreshClaims, now, refreshExp, creds));

        return new TokenPair(access, accessExp, refresh, refreshExp);
    }

    private string RefreshAudience => _o.Audience + ".refresh";
    private string FaceAudience => _o.Audience + ".face";

    /// <summary>Token ngắn hạn chứng nhận khuôn mặt vừa khớp; check-in dùng để tự tính faceMatched.</summary>
    public string CreateFaceToken(int userId, int seconds)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("typ", "face"),
        };
        var creds = new SigningCredentials(CreateKey(_o.Key), SecurityAlgorithms.HmacSha256);
        return new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(_o.Issuer, FaceAudience, claims, now, now.AddSeconds(seconds), creds));
    }

    /// <summary>True nếu faceToken còn hạn, ký đúng và cấp cho đúng userId.</summary>
    public bool ValidateFaceToken(string? token, int userId)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        try
        {
            var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _o.Issuer,
                ValidateAudience = true,
                ValidAudience = FaceAudience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = CreateKey(_o.Key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out _);
            return principal.FindFirst("typ")?.Value == "face"
                && principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value == userId.ToString();
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Kiểm tra refresh token (chữ ký, hạn, audience riêng); trả userId hoặc null.</summary>
    public int? ValidateRefreshToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _o.Issuer,
                ValidateAudience = true,
                ValidAudience = RefreshAudience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = CreateKey(_o.Key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            }, out _);
            if (principal.FindFirst("typ")?.Value != "refresh") return null;
            return int.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
