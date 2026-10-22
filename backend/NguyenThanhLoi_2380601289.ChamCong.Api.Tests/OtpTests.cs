using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Messaging;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Otp;
using Opt = Microsoft.Extensions.Options.Options;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Tests;

public class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> Sent { get; } = [];
    public bool Fail { get; set; }

    public Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        if (Fail) throw AppException.BadGateway("smtp loi");
        Sent.Add((to, subject, body));
        return Task.CompletedTask;
    }
}

public class OtpServiceTests
{
    private readonly AppDbContext _db;
    private readonly FakeSmsSender _sms = new();
    private readonly FakeEmailSender _mail = new();
    private readonly OtpService _svc;
    private readonly User _user;

    public OtpServiceTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _user = new User { Username = "u", FullName = "U", PasswordHash = "x", Email = "u@x.com" };
        _db.Users.Add(_user);
        _db.SaveChanges();
        _svc = new OtpService(_db, _mail, _sms,
            Opt.Create(new OtpOptions { HashPepper = "pepper", ExpireMinutes = 5, MaxAttempts = 5, ResendSeconds = 60 }));
    }

    private static string CodeIn(string text) => Regex.Match(text, @"\d{6}").Value;

    [Fact]
    public async Task Sms_SendsNormalizedPhone_AndStoresOnlyHash()
    {
        var id = await _svc.CreateAndSendAsync(_user, OtpChannel.Sms, "0912345678", OtpPurpose.Login);
        var (phone, text) = _sms.Sent.Single();
        var code = CodeIn(text);
        var otp = await _db.OtpCodes.SingleAsync();

        Assert.Equal("+84912345678", phone);
        Assert.Equal("+84912345678", otp.Destination);
        Assert.NotEqual(code, otp.CodeHash);
        Assert.DoesNotContain(code, otp.CodeHash);
        Assert.Equal("fake-1", otp.GatewayMessageId);
        Assert.StartsWith("[ChamCong]", text);
        Assert.True(text.Length <= 160);
        Assert.DoesNotContain("http", text);
        Assert.All(text, c => Assert.True(c < 128));

        var verified = await _svc.VerifyAsync(id, code);
        Assert.Equal(_user.Id, verified.UserId);
    }

    [Fact]
    public async Task Email_SendsMessage()
    {
        await _svc.CreateAndSendAsync(_user, OtpChannel.Email, "u@x.com", OtpPurpose.Register);
        var m = _mail.Sent.Single();
        Assert.Equal("u@x.com", m.To);
        Assert.Matches(@"^\[ChamCong\] Ma OTP cua ban: \d{6}\. Het han sau 5 phut\.$", m.Body);
    }

    [Fact]
    public async Task WrongCode_FiveTimes_CancelsOtp_EvenForCorrectCode()
    {
        var id = await _svc.CreateAndSendAsync(_user, OtpChannel.Sms, "0912345678", OtpPurpose.Login);
        var code = CodeIn(_sms.Sent[0].Text);
        var wrong = code == "000000" ? "111111" : "000000";

        for (var i = 0; i < 4; i++)
            Assert.Equal(400, (await Assert.ThrowsAsync<AppException>(() => _svc.VerifyAsync(id, wrong))).StatusCode);
        Assert.Equal(429, (await Assert.ThrowsAsync<AppException>(() => _svc.VerifyAsync(id, wrong))).StatusCode);
        Assert.True((await _db.OtpCodes.SingleAsync()).IsUsed);
        await Assert.ThrowsAsync<AppException>(() => _svc.VerifyAsync(id, code));
    }

    [Fact]
    public async Task Otp_IsSingleUse()
    {
        var id = await _svc.CreateAndSendAsync(_user, OtpChannel.Sms, "0912345678", OtpPurpose.Login);
        var code = CodeIn(_sms.Sent[0].Text);
        await _svc.VerifyAsync(id, code);
        await Assert.ThrowsAsync<AppException>(() => _svc.VerifyAsync(id, code));
    }

    [Fact]
    public async Task Otp_Expired_IsRejected()
    {
        var id = await _svc.CreateAndSendAsync(_user, OtpChannel.Sms, "0912345678", OtpPurpose.Login);
        var code = CodeIn(_sms.Sent[0].Text);
        var otp = await _db.OtpCodes.SingleAsync();
        otp.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await _db.SaveChangesAsync();
        var ex = await Assert.ThrowsAsync<AppException>(() => _svc.VerifyAsync(id, code));
        Assert.Contains("het han", ex.Message);
    }

    [Fact]
    public async Task Resend_Within60s_Returns429_ButAfterwardsOk()
    {
        await _svc.CreateAndSendAsync(_user, OtpChannel.Sms, "0912345678", OtpPurpose.Login);
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _svc.CreateAndSendAsync(_user, OtpChannel.Sms, "0912345678", OtpPurpose.Login));
        Assert.Equal(429, ex.StatusCode);

        (await _db.OtpCodes.SingleAsync()).CreatedAt = DateTime.UtcNow.AddSeconds(-61);
        await _db.SaveChangesAsync();
        await _svc.CreateAndSendAsync(_user, OtpChannel.Sms, "0912345678", OtpPurpose.Login);
        Assert.Equal(2, _sms.Sent.Count);
    }

    [Fact]
    public async Task SmsFailure_Returns502_CancelsOtp_AndAllowsImmediateRetry()
    {
        _sms.Fail = true;
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _svc.CreateAndSendAsync(_user, OtpChannel.Sms, "0912345678", OtpPurpose.Login));
        Assert.Equal(502, ex.StatusCode);
        Assert.True((await _db.OtpCodes.SingleAsync()).IsUsed);

        _sms.Fail = false;
        await _svc.CreateAndSendAsync(_user, OtpChannel.Sms, "0912345678", OtpPurpose.Login);
    }

    [Fact]
    public async Task EmailFailure_Returns502_CancelsOtp()
    {
        _mail.Fail = true;
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _svc.CreateAndSendAsync(_user, OtpChannel.Email, "u@x.com", OtpPurpose.Login));
        Assert.Equal(502, ex.StatusCode);
        Assert.True((await _db.OtpCodes.SingleAsync()).IsUsed);
    }

    [Fact]
    public async Task InvalidPhone_Returns400_AndCreatesNothing()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _svc.CreateAndSendAsync(_user, OtpChannel.Sms, "12345", OtpPurpose.Login));
        Assert.Equal(400, ex.StatusCode);
        Assert.Empty(_db.OtpCodes);
    }
}

public class PhoneNormalizerTests
{
    [Theory]
    [InlineData("0912345678", "+84912345678")]
    [InlineData("+84912345678", "+84912345678")]
    [InlineData("091 234 5678", "+84912345678")]
    public void Valid(string input, string expected) => Assert.Equal(expected, PhoneNormalizer.Normalize(input));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("12345")]
    [InlineData("0112345678")]
    [InlineData("+8491234567")]
    [InlineData("abc")]
    public void Invalid_Throws400(string? input) =>
        Assert.Equal(400, Assert.Throws<AppException>(() => PhoneNormalizer.Normalize(input)).StatusCode);
}

public class SmsGatewaySenderTests
{
    private class Handler(Func<HttpRequestMessage, HttpResponseMessage> f) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Url, string? Body, string? Auth)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            Calls.Add((r.Method, r.RequestUri!.ToString(),
                r.Content is null ? null : await r.Content.ReadAsStringAsync(ct), r.Headers.Authorization?.ToString()));
            return f(r);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode code, string json) =>
        new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static SmsGatewaySender Make(Handler h, int pollMax = 3) => new(new HttpClient(h),
        Opt.Create(new SmsGatewayOptions
        {
            BaseUrl = "http://gw:8080/", Username = "user", Password = "pw", SimNumber = 1,
            PollIntervalSeconds = 0, PollMaxSeconds = pollMax,
        }),
        NullLogger<SmsGatewaySender>.Instance);

    [Fact]
    public async Task PendingThenSent_Succeeds_AndSendsCorrectRequest()
    {
        var polls = 0;
        var h = new Handler(r => r.Method == HttpMethod.Post
            ? Json(HttpStatusCode.Accepted, """{"id":"abc","state":"Pending"}""")
            : Json(HttpStatusCode.OK, ++polls < 2 ? """{"id":"abc","state":"Processed"}""" : """{"id":"abc","state":"Sent"}"""));

        var res = await Make(h).SendAsync("+84912345678", "hello", default);

        Assert.True(res.Success);
        Assert.Equal("abc", res.GatewayMessageId);
        Assert.Equal("Sent", res.State);
        var post = h.Calls[0];
        Assert.Equal("http://gw:8080/messages", post.Url);
        Assert.Equal("Basic " + Convert.ToBase64String("user:pw"u8.ToArray()), post.Auth);
        using var body = System.Text.Json.JsonDocument.Parse(post.Body!);
        Assert.Equal("hello", body.RootElement.GetProperty("textMessage").GetProperty("text").GetString());
        Assert.Equal("+84912345678", body.RootElement.GetProperty("phoneNumbers")[0].GetString());
        Assert.Equal(1, body.RootElement.GetProperty("simNumber").GetInt32());
        Assert.Equal("http://gw:8080/messages/abc", h.Calls[1].Url);
    }

    [Fact]
    public async Task Delivered_Succeeds()
    {
        var h = new Handler(r => r.Method == HttpMethod.Post
            ? Json(HttpStatusCode.Accepted, """{"id":"abc"}""")
            : Json(HttpStatusCode.OK, """{"id":"abc","state":"Delivered"}"""));
        Assert.True((await Make(h).SendAsync("+84912345678", "x", default)).Success);
    }

    [Fact]
    public async Task Failed_ReturnsRecipientError()
    {
        var h = new Handler(r => r.Method == HttpMethod.Post
            ? Json(HttpStatusCode.Accepted, """{"id":"abc"}""")
            : Json(HttpStatusCode.OK, """{"id":"abc","state":"Failed","recipients":[{"phoneNumber":"+84","state":"Failed","error":"RESULT_RIL_SMS_SEND_FAIL_RETRY"}]}"""));
        var res = await Make(h).SendAsync("+84912345678", "x", default);
        Assert.False(res.Success);
        Assert.Equal("RESULT_RIL_SMS_SEND_FAIL_RETRY", res.Error);
    }

    [Fact]
    public async Task StuckPending_TimesOut()
    {
        var h = new Handler(r => r.Method == HttpMethod.Post
            ? Json(HttpStatusCode.Accepted, """{"id":"abc"}""")
            : Json(HttpStatusCode.OK, """{"id":"abc","state":"Pending"}"""));
        var res = await Make(h, pollMax: 1).SendAsync("+84912345678", "x", default);
        Assert.False(res.Success);
        Assert.Contains("timeout", res.Error);
    }

    [Fact]
    public async Task Non202_Fails_WithoutPolling()
    {
        var h = new Handler(_ => Json(HttpStatusCode.Unauthorized, "{}"));
        var res = await Make(h).SendAsync("+84912345678", "x", default);
        Assert.False(res.Success);
        Assert.Single(h.Calls);
    }

    [Fact]
    public async Task GatewayUnreachable_ReturnsFailure()
    {
        var h = new Handler(_ => throw new HttpRequestException("connection refused"));
        var res = await Make(h).SendAsync("+84912345678", "x", default);
        Assert.False(res.Success);
        Assert.Contains("connection refused", res.Error);
    }

    [Fact]
    public async Task MissingBaseUrl_ReturnsFailure()
    {
        var s = new SmsGatewaySender(new HttpClient(), Opt.Create(new SmsGatewayOptions()), NullLogger<SmsGatewaySender>.Instance);
        Assert.False((await s.SendAsync("+84912345678", "x", default)).Success);
    }
}
