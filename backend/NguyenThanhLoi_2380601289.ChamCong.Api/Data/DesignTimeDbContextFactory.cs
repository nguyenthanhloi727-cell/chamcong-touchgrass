using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Data;

/// <summary>
/// Dùng riêng cho dotnet ef / Package Manager Console (add-migration, update-database):
/// chỉ cần chuỗi kết nối, không khởi động cả app nên không đòi Jwt:Key, Qr:Secret...
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cfg = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = cfg.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "Thieu ConnectionStrings:Default (dien trong appsettings.Development.json).");

        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs).Options);
    }
}
