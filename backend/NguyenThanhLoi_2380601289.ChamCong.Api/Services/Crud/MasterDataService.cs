using Microsoft.EntityFrameworkCore;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud;

/// <summary>CRUD phòng ban, chức vụ, ca làm, địa điểm. Xóa bản ghi đang được tham chiếu → 409.</summary>
public class MasterDataService(AppDbContext db)
{
    // ---------- Phòng ban ----------

    public async Task<List<DepartmentDto>> ListDepartmentsAsync(string? q, CancellationToken ct)
    {
        var d = db.Departments.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) { q = q.Trim(); d = d.Where(x => x.Name.Contains(q)); }
        return await d.OrderBy(x => x.Id).Select(x => new DepartmentDto(
            x.Id, x.Name, x.ManagerId, x.Manager != null ? x.Manager.FullName : null,
            db.Users.Count(u => u.DepartmentId == x.Id))).ToListAsync(ct);
    }

    public async Task<DepartmentDto> GetDepartmentAsync(int id, CancellationToken ct)
    {
        var d = await db.Departments.AsNoTracking().Where(x => x.Id == id).Select(x => new DepartmentDto(
            x.Id, x.Name, x.ManagerId, x.Manager != null ? x.Manager.FullName : null,
            db.Users.Count(u => u.DepartmentId == x.Id))).FirstOrDefaultAsync(ct);
        return d ?? throw AppException.NotFound("Khong tim thay phong ban");
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(DepartmentRequest req, CancellationToken ct)
    {
        var name = req.Name.Trim();
        await EnsureDepartmentValidAsync(null, name, req.ManagerId, ct);
        var d = new Department { Name = name, ManagerId = req.ManagerId };
        db.Departments.Add(d);
        await db.SaveChangesAsync(ct);
        return await GetDepartmentAsync(d.Id, ct);
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(int id, DepartmentRequest req, CancellationToken ct)
    {
        var d = await db.Departments.FindAsync([id], ct) ?? throw AppException.NotFound("Khong tim thay phong ban");
        var name = req.Name.Trim();
        await EnsureDepartmentValidAsync(id, name, req.ManagerId, ct);
        d.Name = name;
        d.ManagerId = req.ManagerId;
        await db.SaveChangesAsync(ct);
        return await GetDepartmentAsync(id, ct);
    }

    public async Task DeleteDepartmentAsync(int id, CancellationToken ct)
    {
        var d = await db.Departments.FindAsync([id], ct) ?? throw AppException.NotFound("Khong tim thay phong ban");
        var n = await db.Users.CountAsync(u => u.DepartmentId == id, ct);
        if (n > 0) throw AppException.Conflict($"Phong ban con {n} nhan vien, hay chuyen ho sang phong khac truoc khi xoa");
        db.Departments.Remove(d);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureDepartmentValidAsync(int? selfId, string name, int? managerId, CancellationToken ct)
    {
        if (await db.Departments.AnyAsync(x => x.Name == name && x.Id != selfId, ct))
            throw AppException.Conflict("Ten phong ban da ton tai");
        if (managerId != null)
        {
            var m = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == managerId, ct)
                ?? throw AppException.BadRequest("Truong phong khong ton tai");
            if (m.Role == Role.Employee) throw AppException.BadRequest("Truong phong phai la Manager hoac Admin");
        }
    }

    // ---------- Chức vụ ----------

    public async Task<List<PositionDto>> ListPositionsAsync(string? q, Role? roleGroup, CancellationToken ct)
    {
        var p = db.Positions.AsNoTracking().AsQueryable();
        if (roleGroup != null) p = p.Where(x => x.RoleGroup == roleGroup);
        if (!string.IsNullOrWhiteSpace(q)) { q = q.Trim(); p = p.Where(x => x.Name.Contains(q)); }
        return await p.OrderBy(x => x.Id).Select(x => new PositionDto(x.Id, x.Name, x.RoleGroup.ToString())).ToListAsync(ct);
    }

    public async Task<PositionDto> GetPositionAsync(int id, CancellationToken ct)
    {
        var p = await db.Positions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Khong tim thay chuc vu");
        return new PositionDto(p.Id, p.Name, p.RoleGroup.ToString());
    }

    public async Task<PositionDto> CreatePositionAsync(PositionRequest req, CancellationToken ct)
    {
        var (name, group) = await ValidatePositionAsync(null, req, ct);
        var p = new Position { Name = name, RoleGroup = group };
        db.Positions.Add(p);
        await db.SaveChangesAsync(ct);
        return new PositionDto(p.Id, p.Name, p.RoleGroup.ToString());
    }

    public async Task<PositionDto> UpdatePositionAsync(int id, PositionRequest req, CancellationToken ct)
    {
        var p = await db.Positions.FindAsync([id], ct) ?? throw AppException.NotFound("Khong tim thay chuc vu");
        var (name, group) = await ValidatePositionAsync(id, req, ct);
        if (group != p.RoleGroup && await db.Users.AnyAsync(u => u.PositionId == id, ct))
            throw AppException.Conflict("Chuc vu dang co nhan vien su dung, khong doi duoc nhom Manager/Employee");
        p.Name = name;
        p.RoleGroup = group;
        await db.SaveChangesAsync(ct);
        return new PositionDto(p.Id, p.Name, p.RoleGroup.ToString());
    }

    public async Task DeletePositionAsync(int id, CancellationToken ct)
    {
        var p = await db.Positions.FindAsync([id], ct) ?? throw AppException.NotFound("Khong tim thay chuc vu");
        var n = await db.Users.CountAsync(u => u.PositionId == id, ct);
        if (n > 0) throw AppException.Conflict($"Chuc vu dang duoc {n} nhan vien su dung");
        db.Positions.Remove(p);
        await db.SaveChangesAsync(ct);
    }

    private async Task<(string Name, Role Group)> ValidatePositionAsync(int? selfId, PositionRequest req, CancellationToken ct)
    {
        var group = req.RoleGroup ?? throw AppException.BadRequest("Thieu nhom chuc vu");
        if (group == Role.Admin) throw AppException.BadRequest("roleGroup phai la Manager hoac Employee");
        var name = req.Name.Trim();
        if (await db.Positions.AnyAsync(x => x.Name == name && x.Id != selfId, ct))
            throw AppException.Conflict("Ten chuc vu da ton tai");
        return (name, group);
    }

    // ---------- Ca làm ----------

    public async Task<List<ShiftDto>> ListShiftsAsync(string? q, CancellationToken ct)
    {
        var s = db.Shifts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) { q = q.Trim(); s = s.Where(x => x.Name.Contains(q)); }
        return await s.OrderBy(x => x.StartTime).ThenBy(x => x.Id)
            .Select(x => new ShiftDto(x.Id, x.Name, x.StartTime, x.EndTime)).ToListAsync(ct);
    }

    public async Task<ShiftDto> GetShiftAsync(int id, CancellationToken ct)
    {
        var s = await db.Shifts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Khong tim thay ca lam");
        return new ShiftDto(s.Id, s.Name, s.StartTime, s.EndTime);
    }

    public async Task<ShiftDto> CreateShiftAsync(ShiftRequest req, CancellationToken ct)
    {
        var (name, start, end) = await ValidateShiftAsync(null, req, ct);
        var s = new Shift { Name = name, StartTime = start, EndTime = end };
        db.Shifts.Add(s);
        await db.SaveChangesAsync(ct);
        return new ShiftDto(s.Id, s.Name, s.StartTime, s.EndTime);
    }

    public async Task<ShiftDto> UpdateShiftAsync(int id, ShiftRequest req, CancellationToken ct)
    {
        var s = await db.Shifts.FindAsync([id], ct) ?? throw AppException.NotFound("Khong tim thay ca lam");
        var (name, start, end) = await ValidateShiftAsync(id, req, ct);
        s.Name = name;
        s.StartTime = start;
        s.EndTime = end;
        await db.SaveChangesAsync(ct);
        return new ShiftDto(s.Id, s.Name, s.StartTime, s.EndTime);
    }

    public async Task DeleteShiftAsync(int id, CancellationToken ct)
    {
        var s = await db.Shifts.FindAsync([id], ct) ?? throw AppException.NotFound("Khong tim thay ca lam");
        var n = await db.Attendances.CountAsync(a => a.ShiftId == id, ct);
        if (n > 0) throw AppException.Conflict($"Ca lam dang co {n} ban ghi cham cong");
        db.Shifts.Remove(s);
        await db.SaveChangesAsync(ct);
    }

    private async Task<(string Name, TimeOnly Start, TimeOnly End)> ValidateShiftAsync(int? selfId, ShiftRequest req, CancellationToken ct)
    {
        var start = req.StartTime ?? throw AppException.BadRequest("Thieu gio bat dau");
        var end = req.EndTime ?? throw AppException.BadRequest("Thieu gio ket thuc");
        if (start >= end) throw AppException.BadRequest("Gio bat dau phai truoc gio ket thuc");
        var name = req.Name.Trim();
        if (await db.Shifts.AnyAsync(x => x.Name == name && x.Id != selfId, ct))
            throw AppException.Conflict("Ten ca lam da ton tai");
        return (name, start, end);
    }

    // ---------- Địa điểm ----------

    public async Task<List<LocationDto>> ListLocationsAsync(string? q, CancellationToken ct)
    {
        var l = db.Locations.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) { q = q.Trim(); l = l.Where(x => x.Name.Contains(q)); }
        return await l.OrderBy(x => x.Id).Select(x => new LocationDto(x.Id, x.Name, x.Lat, x.Lng, x.RadiusMeters)).ToListAsync(ct);
    }

    public async Task<LocationDto> GetLocationAsync(int id, CancellationToken ct)
    {
        var l = await db.Locations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Khong tim thay dia diem");
        return new LocationDto(l.Id, l.Name, l.Lat, l.Lng, l.RadiusMeters);
    }

    public async Task<LocationDto> CreateLocationAsync(LocationRequest req, CancellationToken ct)
    {
        var name = req.Name.Trim();
        if (await db.Locations.AnyAsync(x => x.Name == name, ct)) throw AppException.Conflict("Ten dia diem da ton tai");
        var l = new Location
        {
            Name = name,
            Lat = req.Lat ?? throw AppException.BadRequest("Thieu vi do"),
            Lng = req.Lng ?? throw AppException.BadRequest("Thieu kinh do"),
            RadiusMeters = req.RadiusMeters,
        };
        db.Locations.Add(l);
        await db.SaveChangesAsync(ct);
        return new LocationDto(l.Id, l.Name, l.Lat, l.Lng, l.RadiusMeters);
    }

    public async Task<LocationDto> UpdateLocationAsync(int id, LocationRequest req, CancellationToken ct)
    {
        var l = await db.Locations.FindAsync([id], ct) ?? throw AppException.NotFound("Khong tim thay dia diem");
        var name = req.Name.Trim();
        if (await db.Locations.AnyAsync(x => x.Name == name && x.Id != id, ct))
            throw AppException.Conflict("Ten dia diem da ton tai");
        l.Name = name;
        l.Lat = req.Lat ?? throw AppException.BadRequest("Thieu vi do");
        l.Lng = req.Lng ?? throw AppException.BadRequest("Thieu kinh do");
        l.RadiusMeters = req.RadiusMeters;
        await db.SaveChangesAsync(ct);
        return new LocationDto(l.Id, l.Name, l.Lat, l.Lng, l.RadiusMeters);
    }

    public async Task DeleteLocationAsync(int id, CancellationToken ct)
    {
        var l = await db.Locations.FindAsync([id], ct) ?? throw AppException.NotFound("Khong tim thay dia diem");
        db.Locations.Remove(l);
        await db.SaveChangesAsync(ct);
    }
}
