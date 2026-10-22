namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Messaging;

public interface IEmailSender
{
    /// <summary>Gửi mail; lỗi → ném AppException 502.</summary>
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}
