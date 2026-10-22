namespace NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneVerified { get; set; }
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public Role Role { get; set; } = Role.Employee;
    public int? PositionId { get; set; }
    public Position? Position { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Pending;
    public string? GoogleSub { get; set; }
    public string? FaceEmbedding { get; set; }
    public double? LastLat { get; set; }
    public double? LastLng { get; set; }
    public DateTime? LastLocationAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
