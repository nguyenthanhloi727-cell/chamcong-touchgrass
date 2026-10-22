using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Otp;

public interface IOtpService
{
    /// <summary>Tạo OTP, gửi qua kênh; trả otpSessionId. Gửi lỗi → OTP bị hủy + 502.</summary>
    Task<int> CreateAndSendAsync(User user, OtpChannel channel, string destination, OtpPurpose purpose, CancellationToken ct = default);

    /// <summary>Kiểm tra mã; đúng → đánh dấu đã dùng và trả bản ghi OTP (kèm User).</summary>
    Task<OtpCode> VerifyAsync(int otpSessionId, string code, CancellationToken ct = default);
}
