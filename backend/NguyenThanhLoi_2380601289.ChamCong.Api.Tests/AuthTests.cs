using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Auth;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Messaging;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Otp;
using Opt = Microsoft.Extensions.Options.Options;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Tests;

public class FakeGoogleVerifier : IGoogleTokenVerifier
{
    public GoogleIdentity? Identity { get; set; }

    public Task<GoogleIdentity> VerifyAsync(string idToken, CancellationToken ct = default) =>
        Identity is null ? throw AppException.Unauthorized("bad token") : Task.FromResult(Identity);
}

public class AuthServiceTests
{
    private readonly AppDbContext _db;
    private readonly FakeSmsSender _sms = new();
    private readonly FakeEmailSender _mail = new();
    private readonly FakeGoogleVerifier _google = new();
    private readonly JwtService _jwt;
    private readonly AuthService _auth;

    public AuthServiceTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var otpOpts = new OtpOptions { HashPepper = "pepper", ExpireMinutes = 5, MaxAttempts = 5, ResendSeconds = 60 };
        _jwt = new JwtService(Opt.Create(new JwtOptions
            { Issuer = "i", Audience = "a", Key = new string('k', 40), AccessMinutes = 15, RefreshDays = 7 }));
        var otps = new OtpService(_db, _mail, _sms, Opt.Create(otpOpts));
        _auth = new AuthService(_db, otps, _jwt, _google, Opt.Create(otpOpts));
    }

    private static string CodeIn(string text) => Regex.Match(text, @"\d{6}").Value;

    private static RegisterRequest Reg(string? email, string? phone, string channel, string username = "newuser") => new()
    {
        Username = username, Password = "Passw0rd!", FullName = "Nguoi Moi",
        Email = email, Phone = phone, VerifyChannel = channel,
    };

    private async Task<User> ActiveUserAsync(string username, string? email = null, bool emailVerified = false,
        string? phone = null, bool phoneVerified = false)
    {
        var u = new User
        {
            Username = username, FullName = username, Email = email, EmailVerified = emailVerified,
            PhoneNumber = phone, PhoneVerified = phoneVerified, Status = UserStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Passw0rd!"),
        };
        _db.Users.Add(u);
        await _db.SaveChangesAsync();
        return u;
    }

    private static async Task<int> StatusOf(Func<Task> act) => (await Assert.ThrowsAsync<AppException>(act)).StatusCode;

    // ----- Đăng ký -----

    [Fact]
    public async Task Register_PhoneOnly_Sms_ThenVerify_ActivatesAndMarksPhoneVerified()
    {
        var s = await _auth.RegisterAsync(Reg(null, "0912345678", "sms"));
        Assert.Equal("sms", s.Channel);
        Assert.Equal("+84*****5678", s.MaskedDestination);
        Assert.Equal("+84912345678", _sms.Sent.Single().Phone);

        var user = await _db.Users.SingleAsync();
        Assert.Equal(UserStatus.Pending, user.Status);
        Assert.Equal(Role.Employee, user.Role);
        Assert.NotEqual("Passw0rd!", user.PasswordHash);

        var r = await _auth.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionId = s.OtpSessionId, Code = CodeIn(_sms.Sent[0].Text) });
        Assert.Equal("Register", r.Purpose);
        Assert.NotNull(r.Auth);
        Assert.True(r.User.PhoneVerified);
        Assert.False(r.User.EmailVerified);
        Assert.Equal(UserStatus.Active, (await _db.Users.SingleAsync()).Status);
    }

    [Fact]
    public async Task Register_EmailOnly_Email_ThenVerify()
    {
        var s = await _auth.RegisterAsync(Reg("Nguyen@Gmail.com", null, "email"));
        Assert.Equal("email", s.Channel);
        Assert.Equal("ng***@gmail.com", s.MaskedDestination);
        Assert.Equal("nguyen@gmail.com", _mail.Sent.Single().To);

        var r = await _auth.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionId = s.OtpSessionId, Code = CodeIn(_mail.Sent[0].Body) });
        Assert.True(r.User.EmailVerified);
        Assert.False(r.User.PhoneVerified);
    }

    [Fact]
    public async Task Register_BothContacts_OnlyChosenChannelVerified()
    {
        var s = await _auth.RegisterAsync(Reg("a@b.com", "0912345678", "email"));
        await _auth.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionId = s.OtpSessionId, Code = CodeIn(_mail.Sent[0].Body) });
        var u = await _db.Users.SingleAsync();
        Assert.True(u.EmailVerified);
        Assert.False(u.PhoneVerified);
        Assert.Equal("+84912345678", u.PhoneNumber);
    }

    [Fact]
    public async Task Register_NoContact_Returns400()
    {
        Assert.Equal(400, await StatusOf(() => _auth.RegisterAsync(Reg(null, null, "email"))));
        Assert.Equal(400, await StatusOf(() => _auth.RegisterAsync(Reg("  ", " ", "sms"))));
        Assert.Empty(_db.Users);
    }

    [Fact]
    public async Task Register_ChannelPointsToMissingContact_Returns400()
    {
        Assert.Equal(400, await StatusOf(() => _auth.RegisterAsync(Reg("a@b.com", null, "sms"))));
        Assert.Equal(400, await StatusOf(() => _auth.RegisterAsync(Reg(null, "0912345678", "email"))));
        Assert.Equal(400, await StatusOf(() => _auth.RegisterAsync(Reg("a@b.com", null, "fax"))));
    }

    [Fact]
    public async Task Register_InvalidEmailOrPhone_Returns400()
    {
        Assert.Equal(400, await StatusOf(() => _auth.RegisterAsync(Reg("khong-phai-email", null, "email"))));
        Assert.Equal(400, await StatusOf(() => _auth.RegisterAsync(Reg(null, "123", "sms"))));
    }

    [Fact]
    public async Task Register_DuplicateUsernameEmailPhone_Returns409()
    {
        await ActiveUserAsync("taken", "x@y.com", true, "+84911111111", true);
        Assert.Equal(409, await StatusOf(() => _auth.RegisterAsync(Reg("new@y.com", null, "email", "taken"))));
        Assert.Equal(409, await StatusOf(() => _auth.RegisterAsync(Reg("X@Y.com", null, "email"))));
        Assert.Equal(409, await StatusOf(() => _auth.RegisterAsync(Reg(null, "0911111111", "sms"))));
    }

    [Fact]
    public async Task Register_SendFails_Returns502_AndAllowsRetryImmediately()
    {
        _sms.Fail = true;
        Assert.Equal(502, await StatusOf(() => _auth.RegisterAsync(Reg(null, "0912345678", "sms"))));
        Assert.Empty(_db.Users);

        _sms.Fail = false;
        await _auth.RegisterAsync(Reg(null, "0912345678", "sms"));
        Assert.Single(_db.Users);
    }

    [Fact]
    public async Task Register_PendingStaleUser_IsReplaced_ButNotWhileOtpFresh()
    {
        await _auth.RegisterAsync(Reg(null, "0912345678", "sms"));
        Assert.Equal(409, await StatusOf(() => _auth.RegisterAsync(Reg(null, "0912345678", "sms"))));

        foreach (var o in _db.OtpCodes) o.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        await _db.SaveChangesAsync();

        await _auth.RegisterAsync(Reg(null, "0912345678", "sms"));
        Assert.Single(_db.Users);
    }

    // ----- Đăng nhập -----

    [Fact]
    public async Task Login_WrongPasswordOrUnknownUser_Returns401()
    {
        await ActiveUserAsync("bob", "b@b.com", true);
        Assert.Equal(401, await StatusOf(() => _auth.LoginAsync(new LoginRequest { Username = "bob", Password = "sai" })));
        Assert.Equal(401, await StatusOf(() => _auth.LoginAsync(new LoginRequest { Username = "nobody", Password = "x" })));
        Assert.Empty(_mail.Sent);
    }

    [Fact]
    public async Task Login_PendingUser_Returns403()
    {
        var u = await ActiveUserAsync("bob", "b@b.com", true);
        u.Status = UserStatus.Pending;
        await _db.SaveChangesAsync();
        Assert.Equal(403, await StatusOf(() => _auth.LoginAsync(new LoginRequest { Username = "bob", Password = "Passw0rd!" })));
    }

    [Fact]
    public async Task Login_VerifiedPhone_UsesSms_EvenWhenEmailAlsoVerified()
    {
        await ActiveUserAsync("bob", "b@b.com", true, "+84912345678", true);
        var s = await _auth.LoginAsync(new LoginRequest { Username = "bob", Password = "Passw0rd!" });
        Assert.Equal("sms", s.Channel);
        Assert.Single(_sms.Sent);
        Assert.Empty(_mail.Sent);
    }

    [Fact]
    public async Task Login_OnlyEmailVerified_UsesEmail_IgnoresUnverifiedPhone()
    {
        await ActiveUserAsync("bob", "b@b.com", true, "+84912345678", false);
        var s = await _auth.LoginAsync(new LoginRequest { Username = "bob", Password = "Passw0rd!" });
        Assert.Equal("email", s.Channel);
        Assert.Empty(_sms.Sent);
    }

    [Fact]
    public async Task Login_Admin_SkipsOtp_ReturnsTokensEvenWithoutVerifiedChannel()
    {
        var u = await ActiveUserAsync("root", "root@chamcong.local", false);
        u.Role = Role.Admin;
        await _db.SaveChangesAsync();

        var s = await _auth.LoginAsync(new LoginRequest { Username = "root", Password = "Passw0rd!" });

        Assert.NotNull(s.Auth);
        Assert.Equal("Admin", s.Auth!.User.Role);
        Assert.Empty(_sms.Sent);
        Assert.Empty(_mail.Sent);
        Assert.Empty(_db.OtpCodes);
        Assert.Equal(401, await StatusOf(() => _auth.LoginAsync(new LoginRequest { Username = "root", Password = "sai" })));
    }

    [Fact]
    public async Task Login_NoVerifiedChannel_Returns403()
    {
        await ActiveUserAsync("bob", "b@b.com", false);
        Assert.Equal(403, await StatusOf(() => _auth.LoginAsync(new LoginRequest { Username = "bob", Password = "Passw0rd!" })));
    }

    [Fact]
    public async Task Login_ThenVerify_ReturnsTokensWithRoleAndPosition()
    {
        var pos = new Position { Name = "Bao ve", RoleGroup = Role.Employee };
        _db.Positions.Add(pos);
        await _db.SaveChangesAsync();
        var u = await ActiveUserAsync("bob", "b@b.com", true);
        u.PositionId = pos.Id;
        await _db.SaveChangesAsync();

        var s = await _auth.LoginAsync(new LoginRequest { Username = "bob", Password = "Passw0rd!" });
        var r = await _auth.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionId = s.OtpSessionId, Code = CodeIn(_mail.Sent[0].Body) });

        Assert.Equal("Login", r.Purpose);
        Assert.Equal("Employee", r.User.Role);
        Assert.Equal("Bao ve", r.User.Position);
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(r.Auth!.AccessToken);
        Assert.Equal(u.Id.ToString(), jwt.Subject);
        Assert.Equal("Employee", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal("Bao ve", jwt.Claims.Single(c => c.Type == "position").Value);
    }

    [Fact]
    public async Task VerifyOtp_WrongFiveTimes_ExhaustsSession()
    {
        await ActiveUserAsync("bob", "b@b.com", true);
        var s = await _auth.LoginAsync(new LoginRequest { Username = "bob", Password = "Passw0rd!" });
        var code = CodeIn(_mail.Sent[0].Body);
        var wrong = code == "000000" ? "111111" : "000000";
        for (var i = 0; i < 4; i++)
            Assert.Equal(400, await StatusOf(() => _auth.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionId = s.OtpSessionId, Code = wrong })));
        Assert.Equal(429, await StatusOf(() => _auth.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionId = s.OtpSessionId, Code = wrong })));
        Assert.Equal(400, await StatusOf(() => _auth.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionId = s.OtpSessionId, Code = code })));
    }

    // ----- Gửi lại OTP -----

    [Fact]
    public async Task Resend_Within60s_Returns429_After60s_NewCodeAndOldOneDies()
    {
        await ActiveUserAsync("bob", "b@b.com", true);
        var s = await _auth.LoginAsync(new LoginRequest { Username = "bob", Password = "Passw0rd!" });
        var oldCode = CodeIn(_mail.Sent[0].Body);

        Assert.Equal(429, await StatusOf(() => _auth.ResendOtpAsync(new ResendOtpRequest { OtpSessionId = s.OtpSessionId })));

        (await _db.OtpCodes.SingleAsync()).CreatedAt = DateTime.UtcNow.AddSeconds(-61);
        await _db.SaveChangesAsync();

        var s2 = await _auth.ResendOtpAsync(new ResendOtpRequest { OtpSessionId = s.OtpSessionId });
        Assert.NotEqual(s.OtpSessionId, s2.OtpSessionId);
        Assert.Equal("email", s2.Channel);
        Assert.Equal(2, _mail.Sent.Count);

        Assert.Equal(400, await StatusOf(() => _auth.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionId = s.OtpSessionId, Code = oldCode })));
        var r = await _auth.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionId = s2.OtpSessionId, Code = CodeIn(_mail.Sent[1].Body) });
        Assert.NotNull(r.Auth);
    }

    [Fact]
    public async Task Resend_AfterSuccessfulVerify_IsRejected()
    {
        await ActiveUserAsync("bob", "b@b.com", true);
        var s = await _auth.LoginAsync(new LoginRequest { Username = "bob", Password = "Passw0rd!" });
        await _auth.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionId = s.OtpSessionId, Code = CodeIn(_mail.Sent[0].Body) });
        (await _db.OtpCodes.SingleAsync()).CreatedAt = DateTime.UtcNow.AddMinutes(-5);
        await _db.SaveChangesAsync();
        Assert.Equal(400, await StatusOf(() => _auth.ResendOtpAsync(new ResendOtpRequest { OtpSessionId = s.OtpSessionId })));
    }

    // ----- Google -----

    [Fact]
    public async Task Google_NewUser_CreatesActiveEmployeeWithVerifiedEmail()
    {
        _google.Identity = new GoogleIdentity("sub-1", "Hello.World@gmail.com", true, "Hello World");
        var r = await _auth.GoogleAsync(new GoogleLoginRequest { IdToken = "t" });
        Assert.Equal("Employee", r.User.Role);
        Assert.True(r.User.EmailVerified);
        Assert.Equal("hello.world@gmail.com", r.User.Email);
        Assert.Equal("hello.world", r.User.Username);
        Assert.Equal(UserStatus.Active, (await _db.Users.SingleAsync()).Status);

        var again = await _auth.GoogleAsync(new GoogleLoginRequest { IdToken = "t" });
        Assert.Equal(r.User.Id, again.User.Id);
        Assert.Single(_db.Users);
    }

    [Fact]
    public async Task Google_LinksExistingVerifiedEmailAccount()
    {
        var u = await ActiveUserAsync("bob", "bob@gmail.com", true);
        _google.Identity = new GoogleIdentity("sub-2", "bob@gmail.com", true, "Bob");
        var r = await _auth.GoogleAsync(new GoogleLoginRequest { IdToken = "t" });
        Assert.Equal(u.Id, r.User.Id);
        Assert.Equal("sub-2", (await _db.Users.SingleAsync()).GoogleSub);
    }

    [Fact]
    public async Task Google_UnverifiedEmailAccountOrUnverifiedGoogleEmail_IsRejected()
    {
        await ActiveUserAsync("bob", "bob@gmail.com", false);
        _google.Identity = new GoogleIdentity("sub-3", "bob@gmail.com", true, "Bob");
        Assert.Equal(409, await StatusOf(() => _auth.GoogleAsync(new GoogleLoginRequest { IdToken = "t" })));

        _google.Identity = new GoogleIdentity("sub-4", "x@gmail.com", false, "X");
        Assert.Equal(401, await StatusOf(() => _auth.GoogleAsync(new GoogleLoginRequest { IdToken = "t" })));
    }

    [Fact]
    public async Task Google_InvalidToken_Returns401()
    {
        _google.Identity = null;
        Assert.Equal(401, await StatusOf(() => _auth.GoogleAsync(new GoogleLoginRequest { IdToken = "t" })));
    }

    // ----- Refresh -----

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewPair_AccessTokenIsRejected()
    {
        var u = await ActiveUserAsync("bob", "b@b.com", true);
        var tokens = _jwt.CreateTokens(u, null);

        var r = await _auth.RefreshAsync(new RefreshRequest { RefreshToken = tokens.RefreshToken });
        Assert.Equal(u.Id, r.User.Id);
        Assert.NotEmpty(r.AccessToken);

        Assert.Equal(401, await StatusOf(() => _auth.RefreshAsync(new RefreshRequest { RefreshToken = tokens.AccessToken })));
        Assert.Equal(401, await StatusOf(() => _auth.RefreshAsync(new RefreshRequest { RefreshToken = "rac" })));
    }

    [Fact]
    public async Task Refresh_LockedUser_Returns401()
    {
        var u = await ActiveUserAsync("bob", "b@b.com", true);
        var tokens = _jwt.CreateTokens(u, null);
        u.Status = UserStatus.Locked;
        await _db.SaveChangesAsync();
        Assert.Equal(401, await StatusOf(() => _auth.RefreshAsync(new RefreshRequest { RefreshToken = tokens.RefreshToken })));
    }

    // ----- Me / bổ sung liên hệ -----

    [Fact]
    public async Task AddContact_Phone_VerifyMarksVerified_AndNextLoginUsesSms()
    {
        var u = await ActiveUserAsync("bob", "b@b.com", true);
        var me = await _auth.GetMeAsync(u.Id);
        Assert.False(me.PhoneVerified);
        Assert.Null(me.PhoneNumber);

        var s = await _auth.AddContactAsync(u.Id, new AddContactRequest { Type = "sms", Value = "0987654321" });
        Assert.Equal("+84*****4321", s.MaskedDestination);
        var r = await _auth.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionId = s.OtpSessionId, Code = CodeIn(_sms.Sent[0].Text) });
        Assert.Equal("AddContact", r.Purpose);
        Assert.Null(r.Auth);
        Assert.True(r.User.PhoneVerified);
        Assert.Equal("+84987654321", r.User.PhoneNumber);

        var login = await _auth.LoginAsync(new LoginRequest { Username = "bob", Password = "Passw0rd!" });
        Assert.Equal("sms", login.Channel);
    }

    [Fact]
    public async Task AddContact_NotAppliedUntilVerified()
    {
        var u = await ActiveUserAsync("bob", "b@b.com", true);
        await _auth.AddContactAsync(u.Id, new AddContactRequest { Type = "sms", Value = "0987654321" });
        var me = await _auth.GetMeAsync(u.Id);
        Assert.Null(me.PhoneNumber);
        Assert.False(me.PhoneVerified);
    }

    [Fact]
    public async Task AddContact_TakenByAnotherUser_Returns409_AndAlreadyVerified_Returns400()
    {
        await ActiveUserAsync("alice", "a@a.com", true, "+84911111111", true);
        var bob = await ActiveUserAsync("bob", "b@b.com", true, "+84922222222", true);

        Assert.Equal(409, await StatusOf(() => _auth.AddContactAsync(bob.Id, new AddContactRequest { Type = "sms", Value = "0911111111" })));
        Assert.Equal(409, await StatusOf(() => _auth.AddContactAsync(bob.Id, new AddContactRequest { Type = "email", Value = "A@a.com" })));
        Assert.Equal(400, await StatusOf(() => _auth.AddContactAsync(bob.Id, new AddContactRequest { Type = "sms", Value = "0922222222" })));
        Assert.Equal(400, await StatusOf(() => _auth.AddContactAsync(bob.Id, new AddContactRequest { Type = "email", Value = "bad" })));
    }

    [Fact]
    public async Task AddContact_ConfirmsExistingUnverifiedEmail()
    {
        var u = await ActiveUserAsync("bob", null, false, "+84922222222", true);
        var s = await _auth.AddContactAsync(u.Id, new AddContactRequest { Type = "email", Value = "bob@x.com" });
        var r = await _auth.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionId = s.OtpSessionId, Code = CodeIn(_mail.Sent[0].Body) });
        Assert.True(r.User.EmailVerified);
        Assert.Equal("bob@x.com", r.User.Email);
    }

    // ----- Mask -----

    [Theory]
    [InlineData("nguyen@gmail.com", "ng***@gmail.com")]
    [InlineData("a@gmail.com", "a***@gmail.com")]
    public void MaskEmail_Works(string input, string expected) => Assert.Equal(expected, AuthService.MaskEmail(input));

    [Fact]
    public void MaskPhone_Works() => Assert.Equal("+84*****1289", AuthService.MaskPhone("+84912341289"));
}
