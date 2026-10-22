using Microsoft.EntityFrameworkCore;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(x => x.Username).HasMaxLength(50).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            e.HasIndex(x => x.PhoneNumber).IsUnique().HasFilter("[PhoneNumber] IS NOT NULL");
            e.ToTable(t => t.HasCheckConstraint("CK_Users_Email_Or_Phone", "[Email] IS NOT NULL OR [PhoneNumber] IS NOT NULL"));
            e.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId);
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
        });

        b.Entity<Department>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId);
        });

        b.Entity<Position>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.RoleGroup).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<Shift>().Property(x => x.Name).HasMaxLength(100).IsRequired();

        b.Entity<Attendance>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Shift).WithMany().HasForeignKey(x => x.ShiftId);
        });

        b.Entity<OtpCode>(e =>
        {
            e.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Destination).HasMaxLength(256).IsRequired();
            e.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
            e.Property(x => x.GatewayMessageId).HasMaxLength(100);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<Location>().Property(x => x.Name).HasMaxLength(100).IsRequired();

        b.Entity<LeaveRequest>(e =>
        {
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ApprovedBy);
        });

        // SQL Server báo "multiple cascade paths" -> mọi FK đều Restrict
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
