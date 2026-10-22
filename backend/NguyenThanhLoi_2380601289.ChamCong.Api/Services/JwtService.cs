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
            new(ClaimTypes.Role, user.Role.ToString()),
            new("position", positionName ?? ""),
        };
        var creds = new SigningCredentials(CreateKey(_o.Key), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_o.Issuer, _o.Audience, claims, now, accessExp, creds);
        var access = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new TokenPair(access, accessExp, CreateRefreshToken(), now.AddDays(_o.RefreshDays));
    }

    public static string CreateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
