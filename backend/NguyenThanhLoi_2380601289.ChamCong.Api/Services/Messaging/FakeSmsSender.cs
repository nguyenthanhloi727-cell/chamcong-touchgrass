namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Messaging;

/// <summary>Chỉ đăng ký trong unit test, KHÔNG đăng ký khi chạy app.</summary>
public class FakeSmsSender(ILogger<FakeSmsSender>? logger = null) : ISmsSender
{
    public List<(string Phone, string Text)> Sent { get; } = [];
    public bool Fail { get; set; }

    public Task<SmsResult> SendAsync(string phone, string text, CancellationToken ct)
    {
        Sent.Add((phone, text));
        logger?.LogInformation("[FakeSms] {Phone}: {Text}", phone, text);
        return Task.FromResult(Fail
            ? new SmsResult(false, null, "Failed", "fake loi")
            : new SmsResult(true, "fake-" + Sent.Count, "Sent", null));
    }
}
