using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Messaging;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Otp;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Auth;

public partial class AuthService(
    AppDbContext db,
    IOtpService otps,
    JwtService jwt,
    IGoogleTokenVerifier google,
    IOptions<OtpOptions> otpOptions)
{
    private static readonly Lazy<string> DummyHash = new(() => BCrypt.Net.BCrypt.HashPassword("dummy-password"));

    [GeneratedRegex(@"[^A-Za-z0-9_.]")]
    private static partial Regex InvalidUsernameChars();

    // ---------- Đăng ký ----------

    public async Task<OtpSessionResponse> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var channel = ParseChannel(req.VerifyChannel);
        var email = NormalizeEmail(req.Email);
        var phone = string.IsNullOrWhiteSpace(req.Phone) ? null : PhoneNormalizer.Normalize(req.Phone);

        if (email is null && phone is null)
            throw AppException.BadRequest("Phai nhap it nhat Email hoac So dien thoai");
        if (channel == OtpChannel.Email && email is null)
            throw AppException.BadRequest("Chon xac nhan qua Email nhung chua nhap Email");
        if (channel == OtpChannel.Sms && phone is null)
            throw AppException.BadRequest("Chon xac nhan qua SMS nhung chua nhap So dien thoai");

        var username = req.Username.Trim();
        await EnsureNoConflictAsync(username, email, phone, ct);

        var user = new User
        {
            Username = username,
            FullName = req.FullName.Trim(),
            Email = email,
            PhoneNumber = phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = Role.Employee,
            Status = UserStatus.Pending,
        };
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw AppException.Conflict("Username, Email hoac So dien thoai da duoc su dung");
        }

        var destination = channel == OtpChannel.Email ? email! : phone!;
        try
        {
            var id = await otps.CreateAndSendAsync(user, channel, destination, OtpPurpose.Register, ct);
            return Session(id, channel, destination);
        }
        catch (AppException)
        {
            // gửi OTP lỗi (502...) → bỏ user Pending để người dùng đăng ký lại được ngay
            await RemoveUsersAsync([user], CancellationToken.None);
            throw;
        }
    }

    // ---------- Xác nhận OTP ----------

    public async Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpRequest req, CancellationToken ct = default)
    {
        var otp = await otps.VerifyAsync(req.OtpSessionId!.Value, req.Code, ct);
        var user = otp.User!;

        switch (otp.Purpose)
        {
            case OtpPurpose.Register:
                if (user.Status == UserStatus.Locked) throw AppException.Forbidden("Tai khoan bi khoa");
                user.Status = UserStatus.Active;
                MarkVerified(user, otp.Channel);
                await db.SaveChangesAsync(ct);
                return new VerifyOtpResponse("Register", await IssueAsync(user, ct), await ToInfoAsync(user, ct));

            case OtpPurpose.Login:
                EnsureActive(user);
                return new VerifyOtpResponse("Login", await IssueAsync(user, ct), await ToInfoAsync(user, ct));

            default: // AddContact
                EnsureActive(user);
                if (otp.Channel == OtpChannel.Email)
                {
                    await EnsureContactFreeAsync(user.Id, otp.Destination, null, ct);
                    user.Email = otp.Destination;
                }
                else
                {
                    await EnsureContactFreeAsync(user.Id, null, otp.Destination, ct);
                    user.PhoneNumber = otp.Destination;
                }
                MarkVerified(user, otp.Channel);
                try
                {
                    await db.SaveChangesAsync(ct);
                }
                catch (DbUpdateException)
                {
                    throw AppException.Conflict("Email hoac So dien thoai da duoc tai khoan khac su dung");
                }
                return new VerifyOtpResponse("AddContact", null, await ToInfoAsync(user, ct));
        }
    }

    public async Task<OtpSessionResponse> ResendOtpAsync(ResendOtpRequest req, CancellationToken ct = default)
    {
        var old = await db.OtpCodes.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == req.OtpSessionId!.Value, ct)
                  ?? throw AppException.BadRequest("Phien OTP khong hop le");
        var o = otpOptions.Value;

        // Mã đã dùng thành công thì không gửi lại; mã bị hủy do nhập sai quá lượt thì vẫn cho xin mã mới
        if (old.IsUsed && old.Attempts < o.MaxAttempts)
            throw AppException.BadRequest("Phien OTP da hoan tat, khong the gui lai");
        if (old.CreatedAt > DateTime.UtcNow.AddSeconds(-o.ResendSeconds))
            throw AppException.TooManyRequests($"Vui long doi {o.ResendSeconds} giay truoc khi gui lai ma");

        var user = old.User!;
        if (old.Purpose == OtpPurpose.Register)
        {
            if (user.Status != UserStatus.Pending) throw AppException.BadRequest("Tai khoan da duoc kich hoat");
        }
        else
        {
            EnsureActive(user);
        }

        var id = await otps.CreateAndSendAsync(user, old.Channel, old.Destination, old.Purpose, ct);
        if (!old.IsUsed)
        {
            old.IsUsed = true; // chỉ mã mới còn hiệu lực
            await db.SaveChangesAsync(ct);
        }
        return Session(id, old.Channel, old.Destination);
    }

    // ---------- Đăng nhập ----------

    public async Task<OtpSessionResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var username = req.Username.Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

        // luôn băm 1 lần để thời gian phản hồi không lộ username có tồn tại hay không
        var passwordOk = BCrypt.Net.BCrypt.Verify(req.Password, user?.PasswordHash ?? DummyHash.Value);
        if (user is null || !passwordOk)
            throw AppException.Unauthorized("Sai ten dang nhap hoac mat khau");

        EnsureActive(user);

        // Admin đăng nhập chỉ cần username + mật khẩu, không qua OTP
        if (user.Role == Role.Admin)
            return new OtpSessionResponse(0, "none", "", await IssueAsync(user, ct));

        OtpChannel channel;
        string destination;
        if (user.PhoneVerified && user.PhoneNumber is not null)
        {
            channel = OtpChannel.Sms;
            destination = user.PhoneNumber;
        }
        else if (user.EmailVerified && user.Email is not null)
        {
            channel = OtpChannel.Email;
            destination = user.Email;
        }
        else
        {
            throw AppException.Forbidden("Tai khoan chua co Email hoac So dien thoai nao duoc xac nhan");
        }

        var id = await otps.CreateAndSendAsync(user, channel, destination, OtpPurpose.Login, ct);
        return Session(id, channel, destination);
    }

    public async Task<AuthResponse> GoogleAsync(GoogleLoginRequest req, CancellationToken ct = default)
    {
        var g = await google.VerifyAsync(req.IdToken, ct);
        if (string.IsNullOrWhiteSpace(g.Email))
            throw AppException.BadRequest("Tai khoan Google khong co email");
        if (!g.EmailVerified)
            throw AppException.Unauthorized("Email Google chua duoc xac minh");
        var email = g.Email.Trim().ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleSub == g.Subject, ct);
        if (user is null)
        {
            var byEmail = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (byEmail is not null && byEmail.Status == UserStatus.Pending)
            {
                await RemoveUsersAsync([byEmail], ct); // chưa xác nhận email → Google xác nhận email này thay
                byEmail = null;
            }

            if (byEmail is not null)
            {
                if (!byEmail.EmailVerified)
                    throw AppException.Conflict("Email nay da gan voi tai khoan chua xac nhan email");
                if (byEmail.GoogleSub is not null)
                    throw AppException.Conflict("Email nay da lien ket voi tai khoan Google khac");
                byEmail.GoogleSub = g.Subject;
                user = byEmail;
            }
            else
            {
                user = new User
                {
                    Username = await GenerateUsernameAsync(email, ct),
                    FullName = string.IsNullOrWhiteSpace(g.Name) ? email.Split('@')[0] : g.Name.Trim(),
                    Email = email,
                    EmailVerified = true,
                    GoogleSub = g.Subject,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))),
                    Role = Role.Employee,
                    Status = UserStatus.Active,
                };
                db.Users.Add(user);
            }
        }

        EnsureActive(user);
        await db.SaveChangesAsync(ct);
        return await IssueAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest req, CancellationToken ct = default)
    {
        var userId = jwt.ValidateRefreshToken(req.RefreshToken)
                     ?? throw AppException.Unauthorized("Refresh token khong hop le hoac da het han");
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || user.Status != UserStatus.Active)
            throw AppException.Unauthorized("Tai khoan khong con hieu luc");
        return await IssueAsync(user, ct);
    }

    // ---------- Hồ sơ ----------

    public async Task<UserInfo> GetMeAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw AppException.Unauthorized("Tai khoan khong ton tai");
        return await ToInfoAsync(user, ct);
    }

    public async Task<OtpSessionResponse> AddContactAsync(int userId, AddContactRequest req, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw AppException.Unauthorized("Tai khoan khong ton tai");
        EnsureActive(user);

        var channel = ParseChannel(req.Type);
        string value;
        if (channel == OtpChannel.Email)
        {
            value = NormalizeEmail(req.Value) ?? throw AppException.BadRequest("Email khong hop le");
            if (user.EmailVerified && user.Email == value)
                throw AppException.BadRequest("Email nay da duoc xac nhan");
            await EnsureContactFreeAsync(userId, value, null, ct);
        }
        else
        {
            value = PhoneNormalizer.Normalize(req.Value);
            if (user.PhoneVerified && user.PhoneNumber == value)
                throw AppException.BadRequest("So dien thoai nay da duoc xac nhan");
            await EnsureContactFreeAsync(userId, null, value, ct);
        }

        var id = await otps.CreateAndSendAsync(user, channel, value, OtpPurpose.AddContact, ct);
        return Session(id, channel, value);
    }

    // ---------- Helpers ----------

    public static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var local = email[..at];
        var shown = local.Length <= 2 ? local[..1] : local[..2];
        return shown + "***" + email[at..];
    }

    public static string MaskPhone(string phone) =>
        phone.Length <= 7 ? "***" : phone[..3] + "*****" + phone[^4..];

    public static string Mask(OtpChannel channel, string destination) =>
        channel == OtpChannel.Email ? MaskEmail(destination) : MaskPhone(destination);

    private static OtpSessionResponse Session(int id, OtpChannel channel, string destination) =>
        new(id, channel == OtpChannel.Email ? "email" : "sms", Mask(channel, destination));

    private static OtpChannel ParseChannel(string? s) => s?.Trim().ToLowerInvariant() switch
    {
        "email" => OtpChannel.Email,
        "sms" => OtpChannel.Sms,
        _ => throw AppException.BadRequest("Kenh phai la \"email\" hoac \"sms\""),
    };

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var e = email.Trim();
        try
        {
            var addr = new MailAddress(e);
            if (addr.Address != e || !addr.Host.Contains('.')) throw new FormatException();
        }
        catch (FormatException)
        {
            throw AppException.BadRequest("Email khong hop le");
        }
        return e.ToLowerInvariant();
    }

    private static void MarkVerified(User user, OtpChannel channel)
    {
        if (channel == OtpChannel.Email) user.EmailVerified = true;
        else user.PhoneVerified = true;
    }

    private static void EnsureActive(User user)
    {
        if (user.Status == UserStatus.Pending) throw AppException.Forbidden("Tai khoan chua duoc kich hoat");
        if (user.Status == UserStatus.Locked) throw AppException.Forbidden("Tai khoan bi khoa");
    }

    private async Task<string> PositionNameAsync(User user, CancellationToken ct) =>
        user.PositionId is null ? "" : await db.Positions.Where(p => p.Id == user.PositionId).Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "";

    private async Task<UserInfo> ToInfoAsync(User u, CancellationToken ct)
    {
        var position = u.PositionId is null ? null : await PositionNameAsync(u, ct);
        return new UserInfo(u.Id, u.Username, u.FullName, u.Email, u.EmailVerified, u.PhoneNumber, u.PhoneVerified,
            u.Role.ToString(), u.PositionId, position, u.DepartmentId, u.FaceEmbedding is not null);
    }

    public async Task<AuthResponse> IssueAsync(User user, CancellationToken ct)
    {
        var t = jwt.CreateTokens(user, await PositionNameAsync(user, ct));
        return new AuthResponse(t.AccessToken, t.AccessExpiresAt, t.RefreshToken, t.RefreshExpiresAt, await ToInfoAsync(user, ct));
    }

    /// <summary>409 nếu trùng; tài khoản Pending (chưa xác nhận) bị thay thế, trừ khi vừa gửi OTP (chống spam SMS).</summary>
    private async Task EnsureNoConflictAsync(string username, string? email, string? phone, CancellationToken ct)
    {
        var matches = await db.Users.Where(u =>
            u.Username == username ||
            (email != null && u.Email == email) ||
            (phone != null && u.PhoneNumber == phone)).ToListAsync(ct);

        foreach (var m in matches.Where(m => m.Status != UserStatus.Pending))
        {
            if (string.Equals(m.Username, username, StringComparison.OrdinalIgnoreCase))
                throw AppException.Conflict("Username da ton tai");
            if (email is not null && m.Email == email) throw AppException.Conflict("Email da duoc su dung");
            throw AppException.Conflict("So dien thoai da duoc su dung");
        }

        if (matches.Count == 0) return;

        var cutoff = DateTime.UtcNow.AddSeconds(-otpOptions.Value.ResendSeconds);
        var ids = matches.Select(m => m.Id).ToList();
        if (await db.OtpCodes.AnyAsync(x => ids.Contains(x.UserId) && x.CreatedAt > cutoff, ct))
            throw AppException.Conflict("Thong tin nay dang cho xac nhan OTP, vui long thu lai sau it phut");

        await RemoveUsersAsync(matches, ct);
    }

    private async Task EnsureContactFreeAsync(int userId, string? email, string? phone, CancellationToken ct)
    {
        if (email is not null && await db.Users.AnyAsync(u => u.Id != userId && u.Email == email, ct))
            throw AppException.Conflict("Email da duoc su dung");
        if (phone is not null && await db.Users.AnyAsync(u => u.Id != userId && u.PhoneNumber == phone, ct))
            throw AppException.Conflict("So dien thoai da duoc su dung");
    }

    private async Task RemoveUsersAsync(List<User> users, CancellationToken ct)
    {
        var ids = users.Select(u => u.Id).ToList();
        db.OtpCodes.RemoveRange(await db.OtpCodes.Where(x => ids.Contains(x.UserId)).ToListAsync(ct));
        db.Users.RemoveRange(users);
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> GenerateUsernameAsync(string email, CancellationToken ct)
    {
        var baseName = InvalidUsernameChars().Replace(email.Split('@')[0], "");
        if (baseName.Length < 3) baseName = "user";
        if (baseName.Length > 40) baseName = baseName[..40];

        var name = baseName;
        while (await db.Users.AnyAsync(u => u.Username == name, ct))
            name = baseName + RandomNumberGenerator.GetInt32(1000, 10000);
        return name;
    }
}
