using System.ComponentModel.DataAnnotations;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;

// ---------- Phòng ban ----------
public class DepartmentRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = "";

    /// <summary>Trưởng phòng (phải là Manager/Admin), có thể bỏ trống.</summary>
    public int? ManagerId { get; set; }
}

public record DepartmentDto(int Id, string Name, int? ManagerId, string? ManagerName, int EmployeeCount);

// ---------- Chức vụ (PositionDto nằm ở EmployeeDtos) ----------
public class PositionRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = "";

    /// <summary>Manager hoặc Employee.</summary>
    [Required] public Role? RoleGroup { get; set; }
}

// ---------- Ca làm ----------
public class ShiftRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required] public TimeOnly? StartTime { get; set; }
    [Required] public TimeOnly? EndTime { get; set; }
}

public record ShiftDto(int Id, string Name, TimeOnly StartTime, TimeOnly EndTime);

// ---------- Địa điểm ----------
public class LocationRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, Range(-90, 90)] public double? Lat { get; set; }
    [Required, Range(-180, 180)] public double? Lng { get; set; }
    [Range(10, 10000, ErrorMessage = "Ban kinh phai tu 10 den 10000 met")]
    public int RadiusMeters { get; set; } = 100;
}

public record LocationDto(int Id, string Name, double Lat, double Lng, int RadiusMeters);

// ---------- Nhân viên (admin tạo / sửa) ----------
public class CreateEmployeeRequest
{
    [Required, StringLength(50, MinimumLength = 3), RegularExpression(@"^[A-Za-z0-9_.]+$", ErrorMessage = "Username chi gom chu, so, dau _ va .")]
    public string Username { get; set; } = "";

    /// <summary>Mật khẩu tạm.</summary>
    [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "Mat khau phai tu 6 ky tu")]
    public string Password { get; set; } = "";

    [Required, StringLength(100)] public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    [Required] public Role? Role { get; set; }
    public int? PositionId { get; set; }
    public int? DepartmentId { get; set; }
}

public class UpdateEmployeeRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int? DepartmentId { get; set; }

    /// <summary>Active hoặc Locked. Bỏ trống = giữ nguyên.</summary>
    public UserStatus? Status { get; set; }

    /// <summary>Đặt lại mật khẩu (bỏ trống = giữ nguyên).</summary>
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mat khau phai tu 6 ky tu")]
    public string? NewPassword { get; set; }
}

// ---------- Chấm công ----------
public class CreateAttendanceRequest
{
    [Required] public int? UserId { get; set; }
    public int? ShiftId { get; set; }
    [Required] public DateTime? CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.OnTime;
}

public class UpdateAttendanceRequest
{
    [Required] public AttendanceStatus? Status { get; set; }
    public DateTime? CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public int? ShiftId { get; set; }
}

public record AttendanceDto(
    int Id, int UserId, string Username, string FullName, int? ShiftId, string? ShiftName,
    DateTime CheckInAt, DateTime? CheckOutAt, double Lat, double Lng,
    bool IsMocked, bool QrValid, bool FaceMatched, int TrustScore, string Status);

// ---------- Đơn nghỉ phép ----------
public class LeaveRequestRequest
{
    [Required] public DateOnly? FromDate { get; set; }
    [Required] public DateOnly? ToDate { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; } = "";
}

public record LeaveRequestDto(
    int Id, int UserId, string Username, string FullName, DateOnly FromDate, DateOnly ToDate,
    string Reason, string Status, int? ApprovedBy);
