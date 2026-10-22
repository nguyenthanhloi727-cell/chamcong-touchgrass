using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Messaging;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Otp;

public class OtpService(
    AppDbContext db,
    IEmailSender email,
    ISmsSender sms,
    IOptions<OtpOptions> options) : IOtpService
{
    private readonly OtpOptions _o = options.Value;

    public async Task<int> CreateAndSendAsync(User user, OtpChannel channel, string destination, OtpPurpose purpose, CancellationToken ct = default)
    {
        if (channel == OtpChannel.Sms) destination = PhoneNormalizer.Normalize(destination);

        // Gửi lại cách ≥ ResendSeconds (chỉ tính OTP còn hiệu lực; OTP gửi lỗi đã bị hủy nên không chặn gửi lại)
        var cutoff = DateTime.UtcNow.AddSeconds(-_o.ResendSeconds);
        var recent = await db.OtpCodes.AnyAsync(x =>
            x.UserId == user.Id && x.Channel == channel && x.Purpose == purpose && !x.IsUsed && x.CreatedAt > cutoff, ct);
        if (recent)
            throw AppException.TooManyRequests($"Vui long doi {_o.ResendSeconds} giay truoc khi gui lai ma");

        var now = DateTime.UtcNow;
        var otp = new OtpCode
        {
            UserId = user.Id,
            Channel = channel,
            Destination = destination,
            Purpose = purpose,
            CodeHash = "",
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_o.ExpireMinutes),
        };
        db.OtpCodes.Add(otp);
        await db.SaveChangesAsync(ct); // cần otp.Id để băm

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        otp.CodeHash = Hash(code, otp.Id);
        await db.SaveChangesAsync(ct);

        string? error = null;
        try
        {
            if (channel == OtpChannel.Email)
            {
                await email.SendAsync(destination, "[ChamCong] Ma OTP", BuildMessage(code), ct);
            }
            else
            {
                var r = await sms.SendAsync(destination, BuildMessage(code), ct);
                otp.GatewayMessageId = r.GatewayMessageId;
                if (!r.Success) error = r.Error ?? "Gateway khong gui duoc SMS";
            }
        }
        catch (AppException ex)
        {
            error = ex.Message;
        }

        if (error is not null)
        {
            otp.IsUsed = true; // không để OTP "treo"
            await db.SaveChangesAsync(CancellationToken.None);
            throw AppException.BadGateway("Khong gui duoc ma OTP: " + error);
        }

        await db.SaveChangesAsync(ct);
        return otp.Id;
    }

    public async Task<OtpCode> VerifyAsync(int otpSessionId, string code, CancellationToken ct = default)
    {
        var otp = await db.OtpCodes.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == otpSessionId, ct)
                  ?? throw AppException.BadRequest("Phien OTP khong hop le");

        if (otp.IsUsed)
            throw AppException.BadRequest("Ma OTP da duoc su dung hoac da het luot nhap");
        if (otp.ExpiresAt <= DateTime.UtcNow)
            throw AppException.BadRequest("Ma OTP da het han");

        var ok = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(code ?? "", otp.Id)),
            Encoding.UTF8.GetBytes(otp.CodeHash));
        if (!ok)
        {
            otp.Attempts++;
            if (otp.Attempts >= _o.MaxAttempts) otp.IsUsed = true;
            await db.SaveChangesAsync(ct);
            throw otp.IsUsed
                ? AppException.TooManyRequests("Nhap sai qua so lan cho phep, ma OTP da bi huy")
                : AppException.BadRequest($"Ma OTP khong dung, con {_o.MaxAttempts - otp.Attempts} lan thu");
        }

        otp.IsUsed = true;
        await db.SaveChangesAsync(ct);
        return otp;
    }

    private string Hash(string code, int otpId) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code + _o.HashPepper + otpId)));

    // Không dấu, ≤160 ký tự, không có link
    private string BuildMessage(string code) =>
        $"[ChamCong] Ma OTP cua ban: {code}. Het han sau {_o.ExpireMinutes} phut.";
}
