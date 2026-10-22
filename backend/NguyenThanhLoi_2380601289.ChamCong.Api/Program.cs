using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NguyenThanhLoi_2380601289.ChamCong.Api.Data;
using NguyenThanhLoi_2380601289.ChamCong.Api.Entities;
using NguyenThanhLoi_2380601289.ChamCong.Api.Middleware;
using NguyenThanhLoi_2380601289.ChamCong.Api.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Auth;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Messaging;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services.Otp;

var builder = WebApplication.CreateBuilder(args);

// Options
var cfg = builder.Configuration;

// Máy mới clone chưa có appsettings.Development.json: tự sinh khóa tạm cho môi trường Development
// (đổi mỗi lần chạy lại → phải đăng nhập lại, mã QR cũ vô hiệu). Production vẫn bắt buộc phải cấu hình.
if (builder.Environment.IsDevelopment())
{
    foreach (var (key, minLength) in new[] { ("Jwt:Key", 32), ("Qr:Secret", 16), ("Otp:HashPepper", 16) })
    {
        if ((cfg[key] ?? "").Length >= minLength) continue;
        cfg[key] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        Console.WriteLine($"[dev] Thieu {key} -> dung khoa tam (dien trong appsettings.Development.json de co dinh).");
    }
}
builder.Services.Configure<JwtOptions>(cfg.GetSection("Jwt"));
builder.Services.Configure<OtpOptions>(cfg.GetSection("Otp"));
builder.Services.Configure<EmailOptions>(cfg.GetSection("Email"));
builder.Services.Configure<SmsGatewayOptions>(cfg.GetSection("SmsGateway"));
builder.Services.Configure<GoogleOptions>(cfg.GetSection("Google"));
builder.Services.Configure<QrOptions>(cfg.GetSection("Qr"));
builder.Services.Configure<GeofenceOptions>(cfg.GetSection("Geofence"));
builder.Services.Configure<FaceOptions>(cfg.GetSection("Face"));

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ctx =>
    {
        // Lỗi đọc JSON (key bắt đầu bằng "$") nói rõ hơn "The req field is required."
        var jsonErr = ctx.ModelState.Where(kv => kv.Key.StartsWith('$') && kv.Value!.Errors.Count > 0)
            .Select(kv => $"Gia tri khong hop le o truong '{kv.Key.TrimStart('$', '.')}'").FirstOrDefault();
        var msg = jsonErr ?? ctx.ModelState.Values.SelectMany(v => v.Errors)
            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Du lieu khong hop le" : e.ErrorMessage)
            .FirstOrDefault() ?? "Du lieu khong hop le";
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { error = msg });
    });
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(cfg.GetConnectionString("Default")));
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddHttpClient<ISmsSender, SmsGatewaySender>(c =>
    c.Timeout = TimeSpan.FromSeconds(cfg.GetValue("SmsGateway:TimeoutSeconds", 10)));
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddSingleton<IGoogleTokenVerifier, GoogleTokenVerifier>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<NguyenThanhLoi_2380601289.ChamCong.Api.Services.Face.FaceService>();
builder.Services.AddScoped<NguyenThanhLoi_2380601289.ChamCong.Api.Services.Rbac.RbacService>();
builder.Services.AddScoped<NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud.MasterDataService>();
builder.Services.AddScoped<NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud.EmployeeAdminService>();
builder.Services.AddScoped<NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud.AttendanceCrudService>();
builder.Services.AddScoped<NguyenThanhLoi_2380601289.ChamCong.Api.Services.Crud.LeaveRequestService>();
builder.Services.AddSingleton<TimeProvider, NguyenThanhLoi_2380601289.ChamCong.Api.Services.Checkin.VietnamTimeProvider>();
builder.Services.AddSingleton<NguyenThanhLoi_2380601289.ChamCong.Api.Services.Checkin.QrService>();
builder.Services.AddScoped<NguyenThanhLoi_2380601289.ChamCong.Api.Services.Checkin.CheckinService>();

// JWT
var jwt = cfg.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key phai co it nhat 32 ky tu (dien trong appsettings.Development.json)");
var qrOpt = cfg.GetSection("Qr").Get<QrOptions>() ?? new QrOptions();
if (qrOpt.Secret.Length < 16 || qrOpt.WindowSeconds <= 0)
    throw new InvalidOperationException("Qr:Secret phai co it nhat 16 ky tu va Qr:WindowSeconds > 0 (dien trong appsettings.Development.json)");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtService.CreateKey(jwt.Key),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role",
        };
        o.MapInboundClaims = false;
        // Role trong token có thể cũ (admin vừa đổi role / khóa tài khoản) -> đối chiếu DB mỗi request
        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var sub = ctx.Principal?.FindFirst("sub")?.Value;
                var role = ctx.Principal?.FindFirst("role")?.Value;
                if (!int.TryParse(sub, out var id)) { ctx.Fail("Token khong hop le"); return; }
                var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var cur = await db.Users.AsNoTracking().Where(u => u.Id == id)
                    .Select(u => new { u.Role, u.Status }).FirstOrDefaultAsync(ctx.HttpContext.RequestAborted);
                if (cur is null || cur.Status != UserStatus.Active || cur.Role.ToString() != role)
                    ctx.Fail("Token het hieu luc (role/trang thai da thay doi), hay dang nhap lai hoac refresh");
            },
        };
    });
builder.Services.AddAuthorization();

// Swagger + nút Authorize (Bearer)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "ChamCong API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Dan access token (khong can go chu Bearer)",
    });
    o.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
    });
});

// CORS mở toàn bộ chỉ ở Development
if (builder.Environment.IsDevelopment())
    builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, app.Configuration, app.Logger);
    var fence = app.Configuration.GetSection("Geofence").Get<GeofenceOptions>() ?? new GeofenceOptions();
    if (fence.Lat == 0 && fence.Lng == 0 && !await db.Locations.AnyAsync())
        app.Logger.LogWarning("Chua cau hinh Geofence:Lat/Lng va chua co Locations -> moi check-in se mat diem GPS");
}

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseCors();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
