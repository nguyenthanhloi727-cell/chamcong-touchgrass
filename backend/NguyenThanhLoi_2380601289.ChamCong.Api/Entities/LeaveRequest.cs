namespace NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

public class LeaveRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public int? ApprovedBy { get; set; }
}
