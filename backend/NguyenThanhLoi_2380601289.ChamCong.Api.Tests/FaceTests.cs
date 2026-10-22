using Microsoft.EntityFrameworkCore;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Auth;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Face;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Messaging;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Otp;
using Opt = Microsoft.Extensions.Options.Options;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Tests;

public class FaceTests
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;
    private readonly FaceService _face;
    private readonly User _u, _locked;

    public FaceTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _jwt = new JwtService(Opt.Create(new JwtOptions { Issuer = "i", Audience = "a", Key = new string('k', 40) }));
        var otpOpts = new OtpOptions { HashPepper = "p" };
        var auth = new AuthService(_db, new OtpService(_db, new FakeEmailSender(), new FakeSmsSender(), Opt.Create(otpOpts)),
            _jwt, new FakeGoogleVerifier(), Opt.Create(otpOpts));
        _face = new FaceService(_db, _jwt, auth, Opt.Create(new FaceOptions { Threshold = 0.8, FaceTokenSeconds = 120 }));
        _u = Add("u1", UserStatus.Active);
        _locked = Add("u2", UserStatus.Locked);
    }

    private User Add(string name, UserStatus st)
    {
        var u = new User { Username = name, FullName = name, Email = name + "@x.com", Role = Role.Employee, Status = st };
        _db.Users.Add(u);
        _db.SaveChanges();
        return u;
    }

    private static float[] Vec(int seed, int n = 128)
    {
        var r = new Random(seed);
        return Enumerable.Range(0, n).Select(_ => (float)(r.NextDouble() * 2 - 1)).ToArray();
    }

    private static async Task<int> StatusOf(Func<Task> act)
    {
        try { await act(); return 200; }
        catch (AppException e) { return e.StatusCode; }
    }

    [Fact]
    public async Task Verify_SameVector_Matches_AndIssuesFaceTokenForThatUserOnly()
    {
        await _face.EnrollAsync(_u.Id, Vec(1), default);
        var r = await _face.VerifyAsync(_u.Id, Vec(1), default);
        Assert.True(r.Matched);
        Assert.InRange(r.Score, 0.999, 1.0001);
        Assert.True(_jwt.ValidateFaceToken(r.FaceToken, _u.Id));
        Assert.False(_jwt.ValidateFaceToken(r.FaceToken, _u.Id + 1));
    }

    [Fact]
    public async Task Verify_SlightNoise_StillMatches_RandomVector_DoesNot()
    {
        var v = Vec(1);
        await _face.EnrollAsync(_u.Id, v, default);
        var noisy = v.Select((x, i) => x + (i % 2 == 0 ? 0.05f : -0.05f)).ToArray();
        Assert.True((await _face.VerifyAsync(_u.Id, noisy, default)).Matched);

        var other = await _face.VerifyAsync(_u.Id, Vec(999), default);
        Assert.False(other.Matched);
        Assert.Null(other.FaceToken);
    }

    [Fact]
    public async Task Verify_Validation()
    {
        Assert.Equal(409, await StatusOf(() => _face.VerifyAsync(_u.Id, Vec(1), default)));      // chưa enroll
        await _face.EnrollAsync(_u.Id, Vec(1), default);
        Assert.Equal(400, await StatusOf(() => _face.VerifyAsync(_u.Id, Vec(1, 64), default)));  // khác chiều
        Assert.Equal(400, await StatusOf(() => _face.VerifyAsync(_u.Id, new float[128], default)));
        Assert.Equal(400, await StatusOf(() => _face.VerifyAsync(_u.Id, null, default)));
        Assert.Equal(400, await StatusOf(() => _face.EnrollAsync(_u.Id, [1f, float.NaN, 1f, 1f, 1f, 1f, 1f, 1f], default)));
        Assert.Equal(400, await StatusOf(() => _face.EnrollAsync(_u.Id, [1f, 2f], default)));
    }

    [Fact]
    public async Task Login_Match_ReturnsJwt_Otherwise401()
    {
        await _face.EnrollAsync(_u.Id, Vec(1), default);
        var ok = await _face.LoginAsync(new FaceLoginRequest { Username = "u1", Embedding = Vec(1) }, default);
        Assert.False(string.IsNullOrEmpty(ok.AccessToken));

        Assert.Equal(401, await StatusOf(() => _face.LoginAsync(new FaceLoginRequest { Username = "u1", Embedding = Vec(2) }, default)));
        Assert.Equal(401, await StatusOf(() => _face.LoginAsync(new FaceLoginRequest { Username = "nobody", Embedding = Vec(1) }, default)));
    }

    [Fact]
    public async Task Login_LockedUser_403()
    {
        await _face.EnrollAsync(_locked.Id, Vec(1), default);
        Assert.Equal(403, await StatusOf(() => _face.LoginAsync(new FaceLoginRequest { Username = "u2", Embedding = Vec(1) }, default)));
    }

    [Fact]
    public async Task FaceToken_Expired_Garbage_AndAccessTokenRejected()
    {
        var shortLived = _jwt.CreateFaceToken(_u.Id, 1);
        Assert.True(_jwt.ValidateFaceToken(shortLived, _u.Id));
        await Task.Delay(1500);
        Assert.False(_jwt.ValidateFaceToken(shortLived, _u.Id));
        Assert.False(_jwt.ValidateFaceToken("abc", _u.Id));
        Assert.False(_jwt.ValidateFaceToken(null, _u.Id));
        Assert.False(_jwt.ValidateFaceToken(_jwt.CreateTokens(_u, null).AccessToken, _u.Id));
    }
}
