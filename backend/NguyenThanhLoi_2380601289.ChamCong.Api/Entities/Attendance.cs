namespace NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

public class Attendance
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int? ShiftId { get; set; }
    public Shift? Shift { get; set; }
    public DateTime CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public bool IsMocked { get; set; }
    public bool QrValid { get; set; }
    public bool FaceMatched { get; set; }
    public int TrustScore { get; set; }
    public AttendanceStatus Status { get; set; }
}
