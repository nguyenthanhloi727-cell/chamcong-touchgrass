namespace NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

public class OtpCode
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public OtpChannel Channel { get; set; }
    public string Destination { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public bool IsUsed { get; set; }
    public string? GatewayMessageId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
