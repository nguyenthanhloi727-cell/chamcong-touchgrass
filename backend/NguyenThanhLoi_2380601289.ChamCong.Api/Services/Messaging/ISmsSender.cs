namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Messaging;

public record SmsResult(bool Success, string? GatewayMessageId, string? State, string? Error);

public interface ISmsSender
{
    /// <param name="phone">Số E.164 (+84...)</param>
    Task<SmsResult> SendAsync(string phone, string text, CancellationToken ct);
}
