using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Options;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Checkin;

/// <summary>QR động: token = base64(window) + "." + HMAC-SHA256(window, Qr:Secret), window = unix_seconds / WindowSeconds.</summary>
public class QrService(IOptions<QrOptions> opt, TimeProvider clock)
{
    private QrOptions O => opt.Value;

    private long CurrentWindow => clock.GetUtcNow().ToUnixTimeSeconds() / O.WindowSeconds;

    public (string Token, int ExpiresIn) Current()
    {
        var w = CurrentWindow;
        var nextStart = (w + 1) * O.WindowSeconds;
        var expiresIn = (int)(nextStart - clock.GetUtcNow().ToUnixTimeSeconds());
        return (Sign(w), expiresIn);
    }

    /// <summary>Hợp lệ nếu chữ ký đúng và window = hiện tại hoặc liền trước (chịu lệch 1 cửa sổ).</summary>
    public bool IsValid(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var parts = token.Split('.');
        if (parts.Length != 2) return false;
        long w;
        try
        {
            if (!long.TryParse(Encoding.UTF8.GetString(Convert.FromBase64String(parts[0])), out w)) return false;
        }
        catch (FormatException) { return false; }

        var expected = Encoding.UTF8.GetBytes(Sign(w));
        var actual = Encoding.UTF8.GetBytes(token);
        if (!CryptographicOperations.FixedTimeEquals(expected, actual)) return false;
        var cur = CurrentWindow;
        return w == cur || w == cur - 1;
    }

    private string Sign(long window)
    {
        if (string.IsNullOrEmpty(O.Secret))
            throw new InvalidOperationException("Qr:Secret chua duoc cau hinh (appsettings.Development.json)");
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(window.ToString()));
        var mac = HMACSHA256.HashData(Encoding.UTF8.GetBytes(O.Secret), Encoding.UTF8.GetBytes(window.ToString()));
        return payload + "." + Convert.ToHexString(mac).ToLowerInvariant();
    }
}
