using System.ComponentModel.DataAnnotations;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Dtos;

public class CheckInRequest
{
    [Required, Range(-90, 90)] public double? Lat { get; set; }
    [Required, Range(-180, 180)] public double? Lng { get; set; }
    public bool IsMocked { get; set; }
    public string? QrToken { get; set; }
}

public record TrustBreakdownDto(int Gps, int Qr, int NotMocked);

public record CheckInResult(int AttendanceId, int TrustScore, TrustBreakdownDto Breakdown, string Status, double DistanceMeters);

public record QrDto(string Token, int ExpiresIn);

public class ReviewRequest
{
    [Required] public bool? Approve { get; set; }
}

public record MonthlyReportRow(int UserId, string Username, string FullName, string? PositionName,
    int WorkDays, int LateCount, int PendingCount, int RejectedCount);

public class ShareLocationRequest
{
    [Required, Range(-90, 90)] public double? Lat { get; set; }
    [Required, Range(-180, 180)] public double? Lng { get; set; }
}

public record TeamLocationDto(int UserId, string Username, string FullName, string? PositionName,
    double Lat, double Lng, DateTime UpdatedAt);
