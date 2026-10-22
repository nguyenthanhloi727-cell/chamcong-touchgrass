using Microsoft.EntityFrameworkCore;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud;

/// <summary>Nhân viên gửi/sửa/rút đơn của mình khi còn Pending; Manager (phòng mình) / Admin duyệt hoặc từ chối.</summary>
public class LeaveRequestService(AppDbContext db)
{
    public const string Pending = "Pending", Approved = "Approved", Rejected = "Rejected";

    private async Task<(User Actor, IQueryable<LeaveRequest> Scope)> ScopeAsync(int actorId, CancellationToken ct)
    {
        var actor = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorId, ct)
            ?? throw AppException.Unauthorized("Tai khoan khong ton tai");
        var q = db.LeaveRequests.Include(l => l.User).AsQueryable();
        q = actor.Role switch
        {
            Role.Admin => q,
            Role.Manager when actor.DepartmentId != null => q.Where(l => l.User!.DepartmentId == actor.DepartmentId),
            _ => q.Where(l => l.UserId == actor.Id),
        };
        return (actor, q);
    }

    public async Task<List<LeaveRequestDto>> ListAsync(int actorId, string? q, string? status, CancellationToken ct)
    {
        var (_, scope) = await ScopeAsync(actorId, ct);
        scope = scope.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = NormalizeStatus(status);
            scope = scope.Where(l => l.Status == s);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            scope = scope.Where(l => l.Reason.Contains(q) || l.User!.Username.Contains(q) || l.User!.FullName.Contains(q));
        }
        return (await scope.OrderByDescending(l => l.Id).ToListAsync(ct)).Select(ToDto).ToList();
    }

    public async Task<LeaveRequestDto> GetAsync(int actorId, int id, CancellationToken ct)
    {
        var (_, scope) = await ScopeAsync(actorId, ct);
        var l = await scope.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Khong tim thay don nghi phep");
        return ToDto(l);
    }

    public async Task<LeaveRequestDto> CreateAsync(int actorId, LeaveRequestRequest req, CancellationToken ct)
    {
        var (from, to) = ValidateDates(req);
        var l = new LeaveRequest { UserId = actorId, FromDate = from, ToDate = to, Reason = req.Reason.Trim(), Status = Pending };
        db.LeaveRequests.Add(l);
        await db.SaveChangesAsync(ct);
        return await GetAsync(actorId, l.Id, ct);
    }

    /// <summary>Chỉ chủ đơn sửa được, và chỉ khi còn Pending.</summary>
    public async Task<LeaveRequestDto> UpdateAsync(int actorId, int id, LeaveRequestRequest req, CancellationToken ct)
    {
        var l = await FindOwnPendingAsync(actorId, id, ct);
        var (from, to) = ValidateDates(req);
        l.FromDate = from;
        l.ToDate = to;
        l.Reason = req.Reason.Trim();
        await db.SaveChangesAsync(ct);
        return await GetAsync(actorId, id, ct);
    }

    /// <summary>Chủ đơn rút đơn Pending; Admin xóa được đơn bất kỳ.</summary>
    public async Task DeleteAsync(int actorId, int id, CancellationToken ct)
    {
        var (actor, scope) = await ScopeAsync(actorId, ct);
        var l = await scope.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Khong tim thay don nghi phep");
        if (actor.Role != Role.Admin)
        {
            if (l.UserId != actor.Id) throw AppException.Forbidden("Chi chu don moi xoa duoc don");
            if (l.Status != Pending) throw AppException.Conflict("Don da duoc xu ly, khong the xoa");
        }
        db.LeaveRequests.Remove(l);
        await db.SaveChangesAsync(ct);
    }

    public Task<LeaveRequestDto> ApproveAsync(int actorId, int id, CancellationToken ct) => DecideAsync(actorId, id, Approved, ct);

    public Task<LeaveRequestDto> RejectAsync(int actorId, int id, CancellationToken ct) => DecideAsync(actorId, id, Rejected, ct);

    private async Task<LeaveRequestDto> DecideAsync(int actorId, int id, string decision, CancellationToken ct)
    {
        var (actor, scope) = await ScopeAsync(actorId, ct);
        if (actor.Role == Role.Employee) throw AppException.Forbidden("Khong co quyen duyet don");
        var l = await scope.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Khong tim thay don nghi phep");
        if (l.UserId == actor.Id) throw AppException.Forbidden("Khong the tu duyet don cua chinh minh");
        if (l.Status != Pending) throw AppException.Conflict($"Don da o trang thai {l.Status}");
        l.Status = decision;
        l.ApprovedBy = actor.Id;
        await db.SaveChangesAsync(ct);
        return ToDto(l);
    }

    private async Task<LeaveRequest> FindOwnPendingAsync(int actorId, int id, CancellationToken ct)
    {
        var l = await db.LeaveRequests.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id && x.UserId == actorId, ct)
            ?? throw AppException.NotFound("Khong tim thay don nghi phep cua ban");
        if (l.Status != Pending) throw AppException.Conflict("Don da duoc xu ly, khong the sua");
        return l;
    }

    private static (DateOnly From, DateOnly To) ValidateDates(LeaveRequestRequest req)
    {
        var from = req.FromDate ?? throw AppException.BadRequest("Thieu ngay bat dau");
        var to = req.ToDate ?? throw AppException.BadRequest("Thieu ngay ket thuc");
        if (to < from) throw AppException.BadRequest("Ngay ket thuc phai tu ngay bat dau tro di");
        return (from, to);
    }

    private static string NormalizeStatus(string s) => s.Trim().ToLowerInvariant() switch
    {
        "pending" => Pending,
        "approved" => Approved,
        "rejected" => Rejected,
        _ => throw AppException.BadRequest("status phai la Pending, Approved hoac Rejected"),
    };

    private static LeaveRequestDto ToDto(LeaveRequest l) => new(
        l.Id, l.UserId, l.User!.Username, l.User.FullName, l.FromDate, l.ToDate, l.Reason, l.Status, l.ApprovedBy);
}
