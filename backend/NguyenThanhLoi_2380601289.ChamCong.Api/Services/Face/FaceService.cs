using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Auth;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Face;

/// <summary>Đăng ký / so khớp khuôn mặt bằng cosine similarity giữa các embedding.</summary>
public class FaceService(AppDbContext db, JwtService jwt, AuthService auth, IOptions<FaceOptions> options)
{
    private const int MinDim = 8, MaxDim = 2048;
    private readonly FaceOptions _o = options.Value;

    public async Task EnrollAsync(int userId, float[]? embedding, CancellationToken ct)
    {
        Validate(embedding);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw AppException.Unauthorized("Tai khoan khong ton tai");
        user.FaceEmbedding = JsonSerializer.Serialize(embedding);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Xóa khuôn mặt đã đăng ký của chính người dùng; chưa đăng ký thì không làm gì (idempotent).</summary>
    public async Task RemoveAsync(int userId, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw AppException.Unauthorized("Tai khoan khong ton tai");
        if (user.FaceEmbedding is null) return;
        user.FaceEmbedding = null;
        await db.SaveChangesAsync(ct);
    }

    public async Task<FaceVerifyResponse> VerifyAsync(int userId, float[]? embedding, CancellationToken ct)
    {
        Validate(embedding);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw AppException.Unauthorized("Tai khoan khong ton tai");
        var stored = Parse(user.FaceEmbedding) ?? throw AppException.Conflict("Chua dang ky khuon mat");
        if (stored.Length != embedding!.Length) throw AppException.BadRequest("Embedding khac so chieu voi khuon mat da dang ky");

        var score = Cosine(stored, embedding);
        if (score < _o.Threshold) return new FaceVerifyResponse(false, Math.Round(score, 4), null, null);
        return new FaceVerifyResponse(true, Math.Round(score, 4), jwt.CreateFaceToken(userId, _o.FaceTokenSeconds), _o.FaceTokenSeconds);
    }

    public async Task<AuthResponse> LoginAsync(FaceLoginRequest req, CancellationToken ct)
    {
        Validate(req.Embedding);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username, ct);
        var stored = user is null ? null : Parse(user.FaceEmbedding);
        // Một thông báo chung: không lộ user có tồn tại / đã đăng ký mặt hay chưa
        if (user is null || stored is null || stored.Length != req.Embedding!.Length || Cosine(stored, req.Embedding) < _o.Threshold)
            throw AppException.Unauthorized("Khuon mat khong khop");
        if (user.Status != UserStatus.Active) throw AppException.Forbidden("Tai khoan chua duoc kich hoat hoac bi khoa");
        return await auth.IssueAsync(user, ct);
    }

    public static double Cosine(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += (double)a[i] * b[i];
            na += (double)a[i] * a[i];
            nb += (double)b[i] * b[i];
        }
        return na == 0 || nb == 0 ? 0 : dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    private static void Validate(float[]? e)
    {
        if (e is null || e.Length < MinDim || e.Length > MaxDim)
            throw AppException.BadRequest($"Embedding phai co tu {MinDim} den {MaxDim} so");
        if (e.Any(x => !float.IsFinite(x))) throw AppException.BadRequest("Embedding chua gia tri khong hop le");
        if (e.All(x => x == 0)) throw AppException.BadRequest("Embedding toan so 0");
    }

    private static float[]? Parse(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<float[]>(json); }
        catch (JsonException) { return null; }
    }
}
