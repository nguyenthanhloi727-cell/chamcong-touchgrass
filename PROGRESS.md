# PROGRESS — App chấm công nhân viên (thực thi trên Claude Code)

> Nhóm **Touch grass** · Dev chính: **Nguyễn Thành Lợi** · Hạn nộp: **22:00 ngày 22/10/2026**
> Nguồn yêu cầu: `PLAN.md` (đọc trước). File này là **danh sách bước làm theo thứ tự**, mỗi bước có: mục tiêu → việc làm → kiểm tra → commit.

---

## Cách dùng file này với Claude Code

1. Đặt `PLAN.md` và `PROGRESS.md` ở **gốc repo**.
2. Mỗi lần giao việc cho Claude Code, dán prompt:

```
Đọc PLAN.md và PROGRESS.md. Bắt đầu từ bước có số nhỏ nhất còn mục chưa tick (gọi là Bước N).

Với mỗi bước, làm đúng thứ tự:
1. Báo: "Đang làm Bước N — <tên bước>".
2. Làm tất cả mục N.x chưa tick (bỏ qua mục ⭐ nếu đã quá thời lượng của bước).
3. Chạy đúng phần "Kiểm tra" của bước; lỗi thì sửa đến khi qua.
4. Tick [x] các mục đã xong, đổi ⬜ → ✅ ở "Bảng tổng tiến độ", ghi 1 dòng vào "Nhật ký" (giờ, bước, kết quả).
5. Commit đúng message ghi trong bước, push thẳng main.
6. Báo: "✅ Xong Bước N. Tiếp theo: Bước N+1 — <tên bước N+1>" rồi LÀM LUÔN Bước N+1, quay lại mục 1.

Dừng lại và hỏi tao khi:
- Kiểm tra cần thao tác của tao (nhận SMS/mail, quét QR, chụp mặt, bấm trên điện thoại) → nói rõ tao cần làm gì, chờ tao báo kết quả rồi mới tick.
- Cần bí mật (mật khẩu SQL, App Password, IP/user/pass gateway, Google Client ID) → bảo tao tự điền vào appsettings.Development.json, không hỏi giá trị trong chat.
- Kẹt 1 lỗi quá 20 phút → báo lỗi + đề xuất phương án cắt.
- Đến 20:30 mà chưa tới Bước 18 → dừng bước đang làm, chuyển sang Bước 18. Đến 21:30 → chuyển sang Bước 19.

Quy tắc chung:
- Không làm lan sang bước khác khi bước hiện tại chưa xong.
- Không commit bí mật (appsettings.Development.json, mật khẩu, IP gateway).
- Chưa chắc API/thư viện thì đọc tài liệu chính thức, không đoán.
```

3. Phiên Claude Code bị ngắt giữa chừng → mở phiên mới, dán lại đúng prompt trên: nó tự tìm bước chưa tick để làm tiếp.

### Ký hiệu
- `[ ]` chưa làm · `[x]` xong · ⭐ tùy chọn (làm nếu dư giờ)
- **[Đề mục X]** = phục vụ mục X trong đề (1 Đăng ký · 2 Đăng nhập · 3 Phân quyền · 4 CRUD · 5 Chức năng nâng cao · 6 Tính năng mới · 7 Giao diện / chạy được)

### Hằng số dùng xuyên suốt

| Tên | Giá trị |
|---|---|
| Thư mục backend | `backend/NguyenThanhLoi_2380601289.ChamCong.Api/` |
| Namespace gốc C# | `NguyenThanhLoi_2380601289.ChamCong.Api` |
| Thư mục Flutter | `mobile/nguyen_thanh_loi_2380601289/` |
| Dart package | `nguyen_thanh_loi_2380601289` |
| applicationId | `com.touchgrass.nguyen_thanh_loi_2380601289` |
| Tên app hiển thị | `Nguyễn Thành Lợi - 2380601289` |
| Cổng backend | `5000` (HTTP, lắng nghe `0.0.0.0`) |
| Database | `ChamCongDb` trên SQL Server, SQL Authentication |
| Tài khoản seed | `admin` / `Admin@123` · `manager1` · `employee1` (mật khẩu ghi trong README) |

---

## Bảng tổng tiến độ

| Bước | Nội dung | Đề mục | Thời lượng | Trạng thái |
|---|---|---|---|---|
| 1 | Khởi tạo repo + 2 project | 7 | 20' | ✅ |
| 2 | CSDL: entity, DbContext, migration, seed | 3, 4 | 40' | ✅ |
| 3 | Nền backend: cấu hình, JWT, Swagger, lỗi | 2, 3 | 25' | ✅ |
| 4 | OTP + gửi Email + gửi SMS qua gateway | 1, 2 | 45' | ✅ |
| 5 | API xác thực: đăng ký, đăng nhập, Google, bổ sung liên hệ | 1, 2 | 45' | ✅ |
| 6 | Phân quyền 2 ô + bảo vệ admin | 3 | 20' | ✅ |
| 7 | API CRUD | 4 | 40' | ✅ |
| 8 | API chấm công: QR, GPS, điểm tin cậy, báo cáo | 5b, 5e, 6 | 40' | ✅ |
| 9 | API khuôn mặt | 5a | 20' | ⬜ |
| 10 | Nền Flutter: package, theme, API client | 7 | 25' | ⬜ |
| 11 | Màn xác thực Flutter | 1, 2 | 50' | ⬜ |
| 12 | Điều hướng theo role + 3 dashboard | 3 | 30' | ⬜ |
| 13 | Màn CRUD (admin) | 4 | 45' | ⬜ |
| 14 | Màn chấm công: GPS + QR + kết quả điểm | 5b, 5e, 6 | 40' | ⬜ |
| 15 | Màn khuôn mặt | 5a | 45' | ⬜ |
| 16 | Màn hồ sơ: bổ sung Email/SĐT | 1 | 20' | ⬜ |
| 17 | `tool/rename.dart` | Luật đặt tên | 25' | ⬜ |
| 18 | Test toàn luồng theo tiêu chí nghiệm thu | Tất cả | 30' | ⬜ |
| 19 | README + đổi tên 4 thành viên + nộp | 7, luật | 30' | ⬜ |

> Tổng ~11 tiếng nếu làm tuần tự → **phải bỏ ⭐ và chạy song song** (vd: trong lúc Claude Code làm backend, mày test gateway / chuẩn bị máy). Mốc cứng: **20:30 bắt đầu Bước 18**, **21:30 bắt đầu Bước 19**.

---

## Bước 0 — Kiểm tra máy trước khi bắt đầu (5')

- [x] 0.1 `dotnet --version` (cần .NET 8 hoặc 10)
- [ ] 0.2 `flutter doctor` (Android toolchain OK, có emulator hoặc máy thật)
- [ ] 0.3 SQL Server đang chạy; đã bật **SQL Server and Windows Authentication mode** + **TCP/IP cổng 1433**; có login dùng cho app
- [ ] 0.4 `git --version`, `gh auth status` (đã đăng nhập GitHub)
- [x] 0.5 `dotnet tool install --global dotnet-ef` (bỏ qua nếu đã có)
- [ ] 0.6 Điện thoại gateway: app SMS Gateway đang chạy Local Server, laptop gọi `GET http://<IP_điện_thoại>:8080/health` → 200

**Kiểm tra:** cả 6 mục OK. Không commit ở bước này.

---

## Bước 1 — Khởi tạo repo + 2 project (20') · [Đề mục 7]

- [x] 1.1 Tạo repo GitHub **public**: https://github.com/nguyenthanhloi727-cell/chamcong-touchgrass
- [x] 1.2 `.gitignore` gộp cho .NET + Flutter, **bắt buộc có**:
  ```
  appsettings.Development.json
  appsettings.Local.json
  *.user
  bin/ obj/
  .env
  build/ .dart_tool/
  ```
- [x] 1.3 Tạo backend (.NET 10 tạo `ChamCong.slnx` thay vì `.sln`):
  ```
  dotnet new webapi -n NguyenThanhLoi_2380601289.ChamCong.Api -o backend/NguyenThanhLoi_2380601289.ChamCong.Api --use-controllers
  dotnet new sln -n ChamCong -o backend
  dotnet sln backend/ChamCong.sln add backend/NguyenThanhLoi_2380601289.ChamCong.Api
  ```
- [x] 1.4 `global.json` ở `backend/` khóa đúng SDK đang cài (`dotnet new globaljson --sdk-version <bản đang cài>`)
- [x] 1.5 Cài package backend (thêm `Microsoft.OpenApi` 2.13.0 + `System.Security.Cryptography.Pkcs` 10.0.13 để vá cảnh báo bảo mật NU1902/NU1903):
  `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next`, `MailKit`, `Google.Apis.Auth`, `Swashbuckle.AspNetCore`
- [x] 1.6 Tạo Flutter (chỉ `--platforms android`):
  ```
  flutter create --org com.touchgrass --project-name nguyen_thanh_loi_2380601289 mobile/nguyen_thanh_loi_2380601289
  ```
- [x] 1.7 Đổi `android:label` thành `Nguyễn Thành Lợi - 2380601289`; kiểm tra `applicationId` = `com.touchgrass.nguyen_thanh_loi_2380601289`
- [x] 1.8 Tạo `backend/.../appsettings.example.json` (khung cấu hình, giá trị rỗng — nội dung ở Bước 3.1)
- [x] 1.9 Copy `PLAN.md`, `PROGRESS.md` vào gốc repo; `README.md` khung (tên nhóm, thành viên, stack)

**Kiểm tra:** `dotnet build backend/ChamCong.sln` OK · `flutter run` ra app mặc định · `git status` không có file bí mật.
**Commit:** `chore: khoi tao repo, backend .NET va Flutter`

---

## Bước 2 — CSDL (40') · [Đề mục 3, 4]

Thư mục: `Data/`, `Entities/`.

- [x] 2.1 Enum: `Role` (Admin, Manager, Employee) · `UserStatus` (Pending, Active, Locked) · `OtpChannel` (Email, Sms) · `OtpPurpose` (Register, Login, AddContact) · `AttendanceStatus` (OnTime, Late, PendingReview). Lưu enum dạng **chuỗi** (`HasConversion<string>()`)
- [x] 2.2 Entity `User`: `Id`, `Username`, `Email?`, `EmailVerified`, `PhoneNumber?`, `PhoneVerified`, `PasswordHash`, `FullName`, `Role`, `PositionId?`, `DepartmentId?`, `Status`, `GoogleSub?`, `FaceEmbedding?` (chuỗi JSON), `CreatedAt`
- [x] 2.3 Entity `Department` (`Id`, `Name`, `ManagerId?`) · `Position` (`Id`, `Name`, `RoleGroup`) · `Shift` (`Id`, `Name`, `StartTime`, `EndTime`)
- [x] 2.4 Entity `Attendance`: `Id`, `UserId`, `ShiftId?`, `CheckInAt`, `CheckOutAt?`, `Lat`, `Lng`, `IsMocked`, `QrValid`, `FaceMatched`, `TrustScore`, `Status`
- [x] 2.5 Entity `OtpCode`: `Id`, `UserId`, `Channel`, `Destination`, `CodeHash`, `Purpose`, `ExpiresAt`, `Attempts`, `IsUsed`, `GatewayMessageId?`, `CreatedAt`
- [x] 2.6 ⭐ Entity `Location` (`Id`, `Name`, `Lat`, `Lng`, `RadiusMeters`) · `LeaveRequest` (`Id`, `UserId`, `FromDate`, `ToDate`, `Reason`, `Status`, `ApprovedBy?`)
- [x] 2.7 `AppDbContext` cấu hình:
  - Unique `Username`
  - Unique **có lọc**: `Email` với `HasFilter("[Email] IS NOT NULL")`, `PhoneNumber` với `HasFilter("[PhoneNumber] IS NOT NULL")`
  - CHECK: `[Email] IS NOT NULL OR [PhoneNumber] IS NOT NULL`
  - FK `User → Position`, `User → Department` (`OnDelete Restrict`)
- [x] 2.8 Seed (`HasData` hoặc `DbSeeder` chạy lúc khởi động):
  - Positions — Manager: Trưởng phòng Nhân sự, Quản lý Kho, Quản lý Tài chính · Employee: Kế toán, Thủ kho, Lễ tân, Bảo vệ, Lao công
  - 1 phòng ban, 1 ca (08:00–17:00)
  - `admin` (Role Admin, mật khẩu từ cấu hình `Seed:AdminPassword`, hash BCrypt, SĐT từ `Seed:AdminPhone`, `PhoneVerified = true`), `manager1`, `employee1`
  - **Không hash BCrypt trong `HasData`** (mỗi lần build ra hash khác → migration đổi liên tục) → seed user bằng `DbSeeder` lúc khởi động
- [x] 2.9 Chuỗi kết nối đọc từ `ConnectionStrings:Default` (`appsettings.Development.json`, có `TrustServerCertificate=True`)
- [x] 2.10 `dotnet ef migrations add Init` → `dotnet ef database update`

**Kiểm tra:** mở SSMS thấy đủ bảng, có index lọc + CHECK; chạy app → có 3 user seed; thử insert user trống cả Email lẫn SĐT → bị chặn.
**Commit:** `feat(db): entity, migration Init, seed admin + chuc vu`

---

## Bước 3 — Nền backend (25') · [Đề mục 2, 3]

- [x] 3.1 Cấu hình (khung trong `appsettings.example.json`, giá trị thật trong `appsettings.Development.json`):
  ```json
  {
    "ConnectionStrings": { "Default": "" },
    "Jwt": { "Issuer": "ChamCong", "Audience": "ChamCongApp", "Key": "", "AccessMinutes": 15, "RefreshDays": 7 },
    "Otp": { "ExpireMinutes": 5, "MaxAttempts": 5, "ResendSeconds": 60, "HashPepper": "" },
    "Email": { "SmtpHost": "smtp.gmail.com", "SmtpPort": 587, "Username": "", "AppPassword": "", "From": "" },
    "SmsGateway": { "BaseUrl": "", "Username": "", "Password": "", "SimNumber": 1, "TimeoutSeconds": 10, "PollIntervalSeconds": 2, "PollMaxSeconds": 15 },
    "Google": { "WebClientId": "" },
    "Qr": { "Secret": "", "WindowSeconds": 30 },
    "Geofence": { "Lat": 0, "Lng": 0, "RadiusMeters": 100 },
    "Seed": { "AdminPassword": "", "AdminPhone": "" }
  }
  ```
  Bind bằng `Options` class (`JwtOptions`, `OtpOptions`, `EmailOptions`, `SmsGatewayOptions`, ...)
- [x] 3.2 JWT: `AddAuthentication().AddJwtBearer(...)`; claim `sub`, `role`, `position`; `JwtService` tạo access + refresh token
- [x] 3.3 Swagger có nút **Authorize** (Bearer)
- [x] 3.4 Middleware bắt lỗi chung → trả `{ "error": "..." }` + mã HTTP đúng (400/401/403/404/409/429/502)
- [x] 3.5 CORS cho phép mọi origin (chỉ môi trường Development)
- [x] 3.6 Lắng nghe `http://0.0.0.0:5000` (`launchSettings.json` hoặc `--urls`); tắt `UseHttpsRedirection` khi Development
- [x] 3.7 `GET /api/health` → `{ "status": "ok" }`

**Kiểm tra:** `dotnet run` → mở `http://localhost:5000/swagger` · từ điện thoại cùng mạng mở `http://<IP-laptop>:5000/api/health` → ok (mở cổng 5000 trên Windows Firewall nếu cần).
**Commit:** `feat(api): cau hinh, JWT, swagger, xu ly loi`

---

## Bước 4 — OTP + gửi Email + gửi SMS (45') · [Đề mục 1, 2]

Thư mục: `Services/Otp/`, `Services/Messaging/`.

- [x] 4.1 `OtpService`:
  - Sinh mã: `RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6")` — **không dùng `System.Random`**
  - Lưu `CodeHash = SHA256(code + Otp:HashPepper + otpId)`; so sánh bằng `CryptographicOperations.FixedTimeEquals`
  - Hết hạn 5', sai tối đa 5 lần (quá → `IsUsed = true`, báo hết lượt), dùng 1 lần, gửi lại cách ≥60s (trả 429 nếu sớm)
  - Hàm: `CreateAndSendAsync(user, channel, destination, purpose)` → trả `otpSessionId` · `VerifyAsync(otpSessionId, code)`
- [x] 4.2 `IEmailSender` + `SmtpEmailSender` (MailKit, Gmail SMTP 587 STARTTLS, App Password). Nội dung: `[ChamCong] Ma OTP cua ban: 123456. Het han sau 5 phut.`
- [x] 4.3 `ISmsSender` + `SmsGatewaySender` (typed `HttpClient`, timeout 10s, Basic Auth):
  - `POST {BaseUrl}/messages` body `{"textMessage":{"text":"..."},"phoneNumbers":["+84..."],"simNumber":1}` → nhận 202 + `id`
  - Poll `GET {BaseUrl}/messages/{id}` mỗi 2s, tối đa 15s
  - Thành công **chỉ khi** `state` = `Sent` hoặc `Delivered`; `Failed` → đọc `recipients[].error`; hết giờ → lỗi timeout
  - Trả `SmsResult { Success, GatewayMessageId, State, Error }`; lưu `GatewayMessageId` vào `OtpCodes`
- [x] 4.4 `PhoneNormalizer`: `0xxxxxxxxx` → `+84xxxxxxxxx`; chấp nhận sẵn `+84`; sai định dạng → 400
- [x] 4.5 Nội dung SMS: không dấu, ≤160 ký tự, có `[ChamCong]`, **không có link**
- [x] 4.6 `FakeSmsSender` (ghi log) **chỉ đăng ký trong unit test**, không dùng khi chạy app
- [x] 4.7 Gửi SMS thất bại → backend trả **502** kèm lý do, không tạo OTP "treo" (đánh dấu `IsUsed = true`)

**Kiểm tra:** endpoint tạm (chỉ Development) `POST /api/dev/sms-test` gửi 1 tin tới SĐT của Lợi → **điện thoại nhận SMS thật**, log ghi `Pending → ... → Sent`; tắt app gateway rồi gửi → nhận lỗi 502 trong ≤15s. Gửi mail test → hộp thư nhận được. **Xóa endpoint tạm** trước khi commit.
**Commit:** `feat(otp): sinh OTP an toan, gui email MailKit, gui SMS qua SMS Gateway + poll trang thai`

---

## Bước 5 — API xác thực (45') · [Đề mục 1, 2]

Controller: `AuthController`, `MeController`.

- [x] 5.1 `POST /api/auth/register` `{username, password, fullName, email?, phone?, verifyChannel: "email"|"sms"}`
  - Thiếu cả email lẫn phone → 400 · `verifyChannel` trỏ tới kênh không nhập → 400
  - Trùng username/email/phone → 409
  - Tạo user `Pending`, `Role = Employee`, mật khẩu BCrypt → gửi OTP qua kênh chọn → trả `{ otpSessionId, channel, maskedDestination }`
- [x] 5.2 `POST /api/auth/verify-otp` `{otpSessionId, code}`
  - `Purpose = Register` → user `Active`, set `EmailVerified` hoặc `PhoneVerified` theo kênh → trả JWT
  - `Purpose = Login` → trả JWT
  - `Purpose = AddContact` → set verified cho kênh mới
- [x] 5.3 `POST /api/auth/login` `{username, password}` → sai → 401 (không nói rõ sai user hay pass) · user chưa `Active` → 403
  - Chọn kênh: `PhoneVerified` → **SMS**; không thì `EmailVerified` → **Email**; không có kênh nào → 403
  - Trả `{ otpSessionId, channel, maskedDestination }`
- [x] 5.4 `POST /api/auth/resend-otp` `{otpSessionId}` → tạo OTP mới cùng kênh, tôn trọng 60s
- [x] 5.5 `POST /api/auth/google` `{idToken}` → `GoogleJsonWebSignature.ValidateAsync` với `Audience = Google:WebClientId` → tìm theo `GoogleSub`/`Email`, chưa có thì tạo `Active`, `EmailVerified = true`, `Role = Employee` → trả JWT
- [x] 5.6 `POST /api/auth/refresh` `{refreshToken}` → cặp token mới
- [x] 5.7 `GET /api/me` → thông tin user + `emailVerified`, `phoneVerified`
- [x] 5.8 `POST /api/me/contact` `{type: "email"|"sms", value}` → gửi OTP `AddContact` · xác nhận qua `/api/auth/verify-otp`
- [x] 5.9 Mask đích gửi: email `ng***@gmail.com`, SĐT `+84*****1289`

**Kiểm tra (Swagger):**
- Đăng ký chỉ SĐT → SMS thật tới máy → verify → `phoneVerified = true`
- Đăng ký chỉ Email → mail tới → verify
- Đăng ký trống cả 2 → 400
- Login user có SĐT đã xác nhận → nhận SMS; login user chỉ có email → nhận mail
- Nhập sai 5 lần → hết lượt; resend trước 60s → 429
- Google: để Bước 11 test cùng app

**Commit:** `feat(auth): dang ky email/sdt, dang nhap + OTP SMS/email, Google, refresh, bo sung lien he`

---

## Bước 6 — Phân quyền 2 ô + bảo vệ admin (20') · [Đề mục 3]

- [x] 6.1 `[Authorize(Roles = "Admin")]` cho quản trị; `[Authorize(Roles = "Admin,Manager")]` cho xem/duyệt phòng ban; `[Authorize]` cho nhân viên
- [x] 6.2 Đổi role: `PUT /api/employees/{id}/role` `{role, positionId}` → chặn nếu `Position.RoleGroup ≠ role`; Admin thì `positionId = null`
- [x] 6.3 Bảo vệ admin: không tự hạ quyền mình, không xóa/hạ admin cuối cùng, không ai tự nâng mình lên Admin → 409/403
- [x] 6.4 Manager chỉ thấy nhân viên cùng `DepartmentId`
- [x] 6.5 `GET /api/positions?roleGroup=Manager|Employee` → cho dropdown ô 2

**Kiểm tra:** đăng nhập `employee1` gọi API admin → 403 · admin tự hạ quyền → bị chặn · gán Kế toán cho Manager → 400.
**Commit:** `feat(rbac): phan quyen 2 o role + chuc vu, bao ve admin`

---

## Bước 7 — API CRUD (40') · [Đề mục 4]

Mỗi resource đủ: `GET list` (có tìm kiếm `?q=`), `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}`; DTO riêng, validate.

- [x] 7.1 `/api/departments`
- [x] 7.2 `/api/positions`
- [x] 7.3 `/api/shifts` (kiểm tra `StartTime < EndTime`)
- [x] 7.4 `/api/employees` (admin tạo nhân viên: username, fullName, email?, phone?, role, positionId, departmentId, mật khẩu tạm) — không trả `PasswordHash`
- [x] 7.5 `/api/attendances` (admin/manager xem + sửa trạng thái; nhân viên chỉ xem của mình)
- [x] 7.6 ⭐ `/api/locations`
- [x] 7.7 ⭐ `/api/leave-requests` + `POST {id}/approve`, `POST {id}/reject` (Manager/Admin)
- [x] 7.8 Xóa đang bị tham chiếu (vd phòng ban còn nhân viên) → 409 kèm thông báo

**Kiểm tra:** Swagger chạy đủ 5 thao tác cho từng resource bằng token admin.
**Commit:** `feat(crud): phong ban, chuc vu, ca lam, nhan vien, cham cong`

---

## Bước 8 — API chấm công (40') · [Đề mục 5b, 5e, 6]

- [x] 8.1 **5e – QR động:** `GET /api/qr/current` (Admin/Manager) → `token = base64(window) + "." + HMAC-SHA256(window, Qr:Secret)`, `window = unix_seconds / 30`; trả `{token, expiresIn}`
- [x] 8.2 Kiểm tra QR: hợp lệ nếu chữ ký đúng và `window` = hiện tại hoặc liền trước (chịu lệch 30s)
- [x] 8.3 **5b – GPS:** khoảng cách Haversine tới `Geofence` (hoặc `Locations` nếu làm 7.6); trong bán kính → `gpsOk`
- [x] 8.4 `POST /api/attendance/check-in` `{lat, lng, isMocked, qrToken, faceMatched}` → chặn check-in 2 lần cùng ca
- [x] 8.5 **6 – Điểm tin cậy** (trọng số đề xuất, chưa chốt): Khuôn mặt khớp **35** · GPS trong vùng **30** · QR hợp lệ **25** · Không GPS giả **10**. `isMocked = true` → điểm GPS = 0
  - `TrustScore ≥ 70` → `OnTime`/`Late` theo giờ ca · `< 70` → `PendingReview`
  - Trả `{trustScore, breakdown: {face, gps, qr, notMocked}, status}`
- [x] 8.6 `POST /api/attendance/check-out`
- [x] 8.7 `POST /api/attendances/{id}/review` `{approve: bool}` (Manager/Admin) cho bản ghi `PendingReview`
- [x] 8.8 `GET /api/reports/monthly?month=YYYY-MM&departmentId=` → mỗi nhân viên: số ngày công, số lần muộn, số lần chờ duyệt
- [x] 8.9 **5b – Chia sẻ vị trí:** `POST /api/location/share` `{lat, lng}` (nhân viên) → lưu `Users.LastLat`, `LastLng`, `LastLocationAt` (thêm 3 cột bằng migration `AddLastLocation`) · `GET /api/location/team` (Manager: nhân viên phòng mình; Admin: tất cả)

**Kiểm tra:** check-in đúng vị trí + QR mới → điểm ≥ 70 · QR cũ hơn 60s → `qr = 0` · `isMocked = true` → `PendingReview`.
**Commit:** `feat(attendance): QR HMAC, geofence, diem tin cay, bao cao thang`

---

## Bước 9 — API khuôn mặt (20') · [Đề mục 5a]

- [ ] 9.1 `POST /api/face/enroll` `{embedding: float[]}` (đã đăng nhập) → lưu JSON vào `Users.FaceEmbedding`
- [ ] 9.2 `POST /api/face/verify` `{embedding}` → cosine similarity với embedding đã lưu → `{matched, score}`; ngưỡng đọc từ cấu hình `Face:Threshold` (giá trị chưa chốt, chỉnh khi test thật ở Bước 15)
- [ ] 9.3 `POST /api/face/login` `{username, embedding}` → khớp → trả JWT (đăng nhập bằng khuôn mặt)
- [ ] 9.4 `face/verify` khớp → trả thêm `faceToken` (JWT ngắn hạn ~2', ký server); `check-in` nhận `faceToken` và tự tính `faceMatched` thay vì tin cờ client gửi (Bước 8 đang tin cờ `faceMatched` của client)

**Kiểm tra:** gửi 2 vector giống nhau → `matched = true`; vector ngẫu nhiên → `false`.
**Commit:** `feat(face): enroll, verify, dang nhap bang khuon mat`

---

## Bước 10 — Nền Flutter (25') · [Đề mục 7]

- [ ] 10.1 Package: `dio`, `flutter_secure_storage`, `go_router`, `google_sign_in`, `geolocator`, `mobile_scanner`, `qr_flutter`, `camera`, `google_mlkit_face_detection`, `local_auth` (⭐ `tflite_flutter` cho embedding)
- [ ] 10.2 Cấu trúc thư mục: `lib/core/` (api, theme, router), `lib/features/<auth|dashboard|crud|attendance|face|profile>/`
- [ ] 10.3 **Import dùng đường dẫn tương đối** (để `tool/rename.dart` đổi tên không hỏng import)
- [ ] 10.4 `BASE_URL` truyền bằng `--dart-define=BASE_URL=http://10.0.2.2:5000` (emulator) hoặc IP LAN (máy thật); có giá trị mặc định
- [ ] 10.5 `ApiClient` (Dio): gắn Bearer token, tự refresh khi 401, đọc `{error}` hiện SnackBar
- [ ] 10.6 Theme Material 3, 1 màu chủ đạo, font mặc định
- [ ] 10.7 Android: `usesCleartextTraffic="true"` **chỉ cho debug** (`src/debug/AndroidManifest.xml`), quyền `INTERNET`, `CAMERA`, `ACCESS_FINE_LOCATION`, `USE_BIOMETRIC`

**Kiểm tra:** app gọi `GET /api/health` hiển thị "ok" trên emulator và máy thật.
**Commit:** `feat(mobile): nen Flutter, api client, theme, router`

---

## Bước 11 — Màn xác thực (50') · [Đề mục 1, 2]

- [ ] 11.1 **Đăng ký:** Username, Họ tên, Mật khẩu, Nhập lại mật khẩu, ô **Email**, ô **SĐT**
  - Validate: không được trống cả Email lẫn SĐT (hiện lỗi ngay dưới 2 ô)
  - Nút "Xác nhận qua Email" chỉ bật khi Email hợp lệ; nút "Xác nhận qua SMS" chỉ bật khi SĐT hợp lệ
- [ ] 11.2 **Màn OTP dùng chung** (đăng ký / đăng nhập / bổ sung liên hệ): **1 ô nhập 6 số**, dòng "Mã đã gửi tới `<maskedDestination>` qua `<SMS|Email>`", nút **Gửi lại** đếm ngược 60s, hiện lỗi sai/hết hạn/hết lượt
- [ ] 11.3 **Đăng nhập:** Username + Mật khẩu → màn OTP → lưu token → điều hướng theo role (Bước 12)
- [ ] 11.4 Nút **Đăng nhập với Google** → `idToken` → `POST /api/auth/google`
  - Cần: OAuth client **Web** (lấy `serverClientId`) + OAuth client **Android** (package + SHA-1) trên Google Cloud Console
- [ ] 11.5 Nút **Đăng nhập bằng khuôn mặt** (mở màn Bước 15)
- [ ] 11.6 Trạng thái loading, chặn bấm 2 lần khi đang gửi OTP (SMS chờ tới 15s)

**Kiểm tra:** đăng ký chỉ SĐT → SMS thật → vào app · đăng ký chỉ Email → mail → vào app · đăng nhập `admin` → SMS → vào trang quản trị · Google đăng nhập được trên máy Lợi.
**Commit:** `feat(mobile-auth): dang ky 2 kenh, OTP 1 o, dang nhap, Google`

---

## Bước 12 — Điều hướng theo role + 3 dashboard (30') · [Đề mục 3]

- [ ] 12.1 Đọc `role` từ JWT → `/admin`, `/manager`, `/employee`; chặn truy cập sai route
- [ ] 12.2 **Admin:** menu Nhân viên, Phòng ban, Chức vụ, Ca làm, Chấm công, Báo cáo, Màn QR
- [ ] 12.3 **Quản lý:** danh sách nhân viên phòng mình, ai đi muộn / chờ duyệt hôm nay, nút Duyệt, Màn QR
- [ ] 12.4 **Nhân viên:** nút **Chấm công** to, ca hôm nay, lịch sử chấm công tháng này, điểm tin cậy lần gần nhất
- [ ] 12.5 Header hiện tên + **Role · Chức vụ** (vd "Nhân viên · Bảo vệ")
- [ ] 12.6 Nút Đăng xuất (xóa token)

**Kiểm tra:** đăng nhập lần lượt `admin`, `manager1`, `employee1` → mỗi người vào đúng màn, không vào được màn người khác.
**Commit:** `feat(mobile-rbac): dieu huong theo role, 3 dashboard`

---

## Bước 13 — Màn CRUD (45') · [Đề mục 4]

Mỗi màn: danh sách + ô tìm kiếm, nút Thêm, bấm vào để Sửa, vuốt/nút để Xóa (hộp xác nhận).

- [ ] 13.1 Phòng ban
- [ ] 13.2 Chức vụ (chọn RoleGroup Quản lý / Nhân viên)
- [ ] 13.3 Ca làm (chọn giờ bằng TimePicker)
- [ ] 13.4 Nhân viên — form có **2 dropdown**: ô 1 **Role** (Quản lý / Nhân viên), ô 2 **Chức vụ** lọc theo ô 1 (đổi ô 1 → reset ô 2); dropdown Phòng ban; Email, SĐT
- [ ] 13.5 Chấm công (admin xem tất cả, lọc theo ngày; sửa trạng thái)
- [ ] 13.6 ⭐ Địa điểm · ⭐ Đơn nghỉ phép (nhân viên gửi, quản lý duyệt)

**Kiểm tra:** thêm/sửa/xóa được ở cả 5 màn; đổi Role ô 1 → ô 2 đổi danh sách đúng.
**Commit:** `feat(mobile-crud): man CRUD + dropdown role/chuc vu`

---

## Bước 14 — Màn chấm công (40') · [Đề mục 5b, 5e, 6]

- [ ] 14.1 **Màn QR (Admin/Quản lý):** gọi `/api/qr/current` mỗi 30s, hiện QR to bằng `qr_flutter` + đồng hồ đếm ngược
- [ ] 14.2 **Luồng Chấm công (Nhân viên):**
  1. Xin quyền vị trí → lấy `lat/lng` + `isMocked` (geolocator)
  2. Quét QR (`mobile_scanner`)
  3. Xác minh khuôn mặt (Bước 15; chưa xong thì `faceMatched = false`)
  4. Gửi `check-in`
- [ ] 14.3 **Màn kết quả (Đề mục 6):** điểm tin cậy to + 4 dòng chi tiết (Khuôn mặt / GPS / QR / Không GPS giả, mỗi dòng ✔/✖ + điểm) + trạng thái
- [ ] 14.4 **5b – Chia sẻ vị trí:** nút "Chia sẻ vị trí hiện tại" gửi `lat/lng` lên server; quản lý xem tọa độ mới nhất của nhân viên (danh sách, ⭐ bản đồ)
- [ ] 14.5 Check-out
- [ ] 14.6 Màn duyệt `PendingReview` cho quản lý (Duyệt / Từ chối)
- [ ] 14.7 Nút **"Đặt vị trí hiện tại làm điểm chấm công"** (Admin, đặt ở màn QR): lấy GPS điện thoại thật (geolocator, độ chính xác cao) → `POST /api/locations` bán kính mặc định **200 m** (GPS trong nhà lệch 20–50 m). Mục tiêu: chạy đúng với **vị trí thật trên máy thật**; emulator không cần hỗ trợ

**Kiểm tra:** máy A mở Màn QR, máy B quét → check-in thành công điểm ≥ 70 · bật GPS giả (nếu thử được) → `PendingReview` → quản lý duyệt được.
**Commit:** `feat(mobile-attendance): GPS, QR dong, diem tin cay, duyet`

---

## Bước 15 — Màn khuôn mặt (45') · [Đề mục 5a]

- [ ] 15.1 Camera trước + ML Kit Face Detection, bật `enableClassification` để lấy xác suất mở mắt
- [ ] 15.2 **Chống ảnh giả:** yêu cầu chớp mắt (mắt mở → nhắm → mở) trong 5s
- [ ] 15.3 **Đăng ký khuôn mặt** (trong Hồ sơ): chụp sau khi qua chớp mắt → tạo embedding → `POST /api/face/enroll`
- [ ] 15.4 Tạo embedding:
  - Chính: ⭐ MobileFaceNet qua `tflite_flutter` (file model `.tflite`: nguồn + license **chưa chốt**, phải ghi rõ trong README)
  - Dự phòng nếu không kịp: vector từ landmark/contour ML Kit đã chuẩn hóa (độ chính xác thấp, ghi rõ trong README)
- [ ] 15.5 **Đăng nhập bằng khuôn mặt:** nhập username → chớp mắt → embedding → `POST /api/face/login`
- [ ] 15.6 Dùng lại ở bước chấm công (14.2.3) → `faceMatched`
- [ ] 15.7 **Phương án cắt:** quá giờ → dùng `local_auth` (vân tay/khóa sinh trắc của máy) cho bước xác minh, ghi rõ trong README

**Kiểm tra:** đăng ký mặt Lợi → đăng nhập bằng mặt Lợi thành công · giơ ảnh tĩnh → không qua bước chớp mắt · mặt người khác → không khớp (chỉnh `Face:Threshold`).
**Commit:** `feat(mobile-face): phat hien mat, chop mat, dang nhap khuon mat`

---

## Bước 16 — Màn hồ sơ: bổ sung Email/SĐT (20') · [Đề mục 1]

- [ ] 16.1 Hiện Email + SĐT, mỗi dòng có nhãn **Đã xác nhận** / **Chưa xác nhận** / **Chưa có**
- [ ] 16.2 Nút "Thêm / Xác nhận" → nhập giá trị → `POST /api/me/contact` → màn OTP dùng chung (11.2)
- [ ] 16.3 Nút "Đăng ký khuôn mặt" (15.3)

**Kiểm tra:** user đăng ký chỉ bằng Email → thêm SĐT → nhận SMS → xác nhận → lần đăng nhập sau OTP đi qua **SMS**.
**Commit:** `feat(mobile-profile): bo sung va xac nhan email/sdt`

---

## Bước 17 — `tool/rename.dart` (25') · [Luật đặt tên]

Chỉ dùng `dart:io`, không cần package. Chạy ở gốc repo:
```
dart run tool/rename.dart --name "Hồ Ngọc Phú" --mssv 2380601699
```

- [ ] 17.1 Bỏ dấu tiếng Việt (bảng thay ký tự, `đ/Đ → d/D`) → `snake` = `ho_ngoc_phu_2380601699`, `Pascal` = `HoNgocPhu_2380601699`
- [ ] 17.2 Tự đọc tên hiện tại (từ `name:` trong `pubspec.yaml` và tên file `.csproj`) — không ghi cứng tên Lợi
- [ ] 17.3 Flutter: `name:` trong `pubspec.yaml`; mọi `package:<cũ>/` trong `lib/`, `test/`
- [ ] 17.4 Android: `applicationId`, `namespace` trong `android/app/build.gradle(.kts)`; `android:label` = `"<Họ tên> - <MSSV>"`; chuyển thư mục `kotlin/com/touchgrass/<cũ>/` → `<mới>/` và sửa dòng `package` trong `MainActivity.kt`
- [ ] 17.5 Đổi tên thư mục `mobile/<cũ>/` → `mobile/<mới>/`
- [ ] 17.6 Backend: đổi tên thư mục + file `.csproj`; sửa đường dẫn trong `ChamCong.sln`; thay `namespace`/`using` trong mọi `.cs`; `RootNamespace` nếu có
- [ ] 17.7 Cuối script: quét toàn repo (trừ `bin/ obj/ build/ .git/`) tìm tên cũ → in số chỗ còn sót (phải = 0)
- [ ] 17.8 Tùy chọn `--dry-run` chỉ in những gì sẽ đổi

**Kiểm tra:** trên 1 bản copy repo, chạy cho Phú → `flutter clean && flutter pub get && flutter run` OK, `dotnet build` OK, quét tên cũ = 0. Chạy ngược lại về tên Lợi → giống ban đầu.
**Commit:** `feat(tool): rename.dart doi ten project theo ho ten + MSSV`

---

## Bước 18 — Test toàn luồng (30') · [Tất cả đề mục]

Trước khi test: gateway `/health` = 200, SIM 1 còn tiền, điện thoại + laptop cùng mạng.

| # | Kịch bản | Đề mục | Kết quả |
|---|---|---|---|
| 18.1 | Đăng ký chỉ SĐT → nhận **SMS thật** → vào app | 1 | ⬜ |
| 18.2 | Đăng ký chỉ Email → nhận mail → vào app | 1 | ⬜ |
| 18.3 | Đăng ký trống cả 2 → bị chặn | 1 | ⬜ |
| 18.4 | Đăng ký bằng Google | 1 | ⬜ |
| 18.5 | Đăng nhập user + pass + OTP SMS | 2 | ⬜ |
| 18.6 | OTP sai 5 lần → hết lượt · quá 5' → hết hạn · gửi lại trước 60s → bị chặn · OTP đã dùng → không dùng lại | 2 | ⬜ |
| 18.7 | Tắt app gateway → app báo lỗi gửi SMS trong ≤15s | 2 | ⬜ |
| 18.8 | `admin` / `manager1` / `employee1` vào đúng màn, gọi API sai quyền → 403 | 3 | ⬜ |
| 18.9 | Dropdown Role → Chức vụ lọc đúng; admin không tự hạ quyền được | 3 | ⬜ |
| 18.10 | CRUD đủ 5 màn | 4 | ⬜ |
| 18.11 | Đăng nhập bằng khuôn mặt + chặn ảnh tĩnh | 5a | ⬜ |
| 18.12 | Check-in ngoài bán kính → mất điểm GPS; chia sẻ vị trí → quản lý thấy | 5b | ⬜ |
| 18.13 | QR mới → hợp lệ; QR cũ → không hợp lệ | 5e | ⬜ |
| 18.14 | Điểm tin cậy thấp → `PendingReview` → quản lý duyệt | 6 | ⬜ |
| 18.15 | DB chỉ có `CodeHash`; repo không có mật khẩu/IP gateway/App Password | Bảo mật | ⬜ |

Lỗi nào sửa ngay; không kịp → ghi vào mục "Hạn chế" trong README.
**Commit:** `test: kiem thu toan luong, sua loi`

---

## Bước 19 — README + đổi tên + nộp (30') · [Đề mục 7, luật]

- [ ] 19.1 README:
  - Tên nhóm, 4 thành viên + MSSV, stack, sơ đồ kiến trúc (Flutter → Backend → SMS Gateway → SIM)
  - Cài đặt: SQL Server (bật SQL Auth + TCP/IP), copy `appsettings.example.json` → `appsettings.Development.json` và điền, `dotnet ef database update`, `dotnet run --urls http://0.0.0.0:5000`, `flutter run --dart-define=BASE_URL=...`
  - Tài khoản demo: `admin` / `Admin@123` + `manager1` + `employee1`
  - Ghi chú: **SMS OTP gửi thật qua server điện thoại của nhóm, demo trên máy nhóm**; Google Sign-In cần SHA-1 đã đăng ký
  - Danh sách chức năng theo 7 mục đề + mục "Tính năng mới: Điểm tin cậy"
  - Hạn chế (nếu có ở Bước 18)
- [ ] 19.2 `git grep` kiểm tra không có: App Password, mật khẩu gateway, IP gateway, JWT key
- [ ] 19.3 Push bản gốc (tên Lợi) lên `main`
- [ ] 19.4 Mỗi thành viên (trên máy/bản copy riêng, **không push lên `main`**):
  ```
  dart run tool/rename.dart --name "Hồ Ngọc Phú" --mssv 2380601699
  dart run tool/rename.dart --name "Phạm Đinh Gia Bảo" --mssv 2380600166
  dart run tool/rename.dart --name "Phương Bảo Khôi" --mssv 2380601104
  ```
  → build + chạy được → nộp theo cách thầy yêu cầu (repo riêng hay chung: **chưa chốt**, xem PLAN.md mục 8)
- [ ] 19.5 Thêm SHA-1 + OAuth client Android cho từng `applicationId` nếu thành viên cần demo Google Sign-In
- [ ] 19.6 **Nộp trước 22:00**

**Commit:** `docs: README, huong dan chay, tai khoan demo`

---

## Nhật ký

| Giờ | Bước | Kết quả / ghi chú |
|---|---|---|
| 14:30 | 0 | dotnet 10, git, dotnet-ef, Flutter OK. Thiếu `gh`; SQL Auth/TCP 1433 + gateway `/health` chưa kiểm tra |
| 14:45 | 1 | Backend + Flutter tạo xong; `dotnet build` 0 cảnh báo; `flutter build apk --debug` OK khi đặt `TEMP=C:\tmp` (TEMP có dấu cách làm Gradle lỗi "Unable to establish loopback connection"). Commit local, chưa push vì chưa có repo |
| 15:10 | 1 | Tạo repo public `nguyenthanhloi727-cell/chamcong-touchgrass`, push `main` OK. Máy là của Phú → remote URL có sẵn username Lợi, không đụng credential của Phú |
| 15:50 | 2 | 8 entity + migration Init + DbSeeder OK. Test bằng `.\SQLEXPRESS` Windows Auth: đủ 8 bảng, 2 index lọc, CHECK chặn user trống Email+SĐT, seed 3 user + 8 chức vụ. `ConnectionStrings:Default` trong appsettings.Development.json còn trống (Lợi tự điền SQL Auth); Seed:*Email/*Phone trống → user seed dùng email giả chưa xác nhận, cần điền trước Bước 5 |
| 16:20 | 3 | Options, JwtService, Swagger Authorize, ErrorHandlingMiddleware, CORS dev, 0.0.0.0:5000, /api/health OK. Điện thoại mở http://172.16.24.207:5000/api/health ra ok sau khi Lợi mở cổng 5000 trên Firewall. Jwt:Key dev tự sinh, ConnectionStrings:Default dev = .\SQLEXPRESS Windows Auth |
| 14:55 | 4 | OtpService, SmtpEmailSender, SmsGatewaySender, PhoneNormalizer, FakeSmsSender + project test (25 test qua). Test thật: SMS tới máy Lợi (gateway Delivered ~2.5s), mail nhận được (Gmail App Password), tắt gateway -> 502 sau ~3s; DB chi co CodeHash, OTP gui loi IsUsed=1. Da xoa endpoint tam /api/dev |
| 15:10 | 5 | AuthService + AuthController/MeController, 56 unit test qua. Test thật: đăng ký chỉ Email → mail OTP → verify → emailVerified; login user chỉ email → OTP mail; SMS OTP thật tới máy Lợi → verify ra JWT admin; sai 5 lần → hủy (429), resend <60s → 429, dùng lại mã → 400, refresh OK. Sửa claim role JWT (trước ghi URI dài, Authorize Roles sẽ không khớp). Refresh token = JWT audience riêng (không thêm bảng). Đăng ký chỉ SĐT chỉ test bằng unit test (SĐT Lợi đã thuộc admin). Google để Bước 11 |
| 17:10 | 6 | RbacService + EmployeesController (GET list/get theo phạm vi, PUT {id}/role) + PositionsController (?roleGroup=). 66 unit test qua. Test HTTP thật bằng JWT ký tay: employee1 → /api/employees & PUT role = 403; manager1 PUT role = 403, xem list chỉ cùng phòng; admin tự hạ = 403, Kế toán cho Manager = 400, Admin kèm chức vụ = 400, đổi hợp lệ = 200. Sửa: thêm JsonStringEnumConverter (enum Role nhận chuỗi). Admin cuối cùng → 409 chỉ test bằng unit test (chỉ có 1 admin thật) |
| 17:40 | 6 | Vá thêm theo rà soát: JwtBearer đối chiếu role/trạng thái với DB mỗi request (token mang role cũ hoặc user bị khóa → 401, refresh ra token đúng role); báo lỗi JSON/enum sai rõ tên trường. Test HTTP: token employee1 giả role Admin → 401, token đúng → 200. Còn lại: Manager mới chưa có phòng ban chỉ thấy chính mình (gán phòng ban ở Bước 7) |
| 15:20 | 7 | CRUD đủ GET list (?q=) / GET {id} / POST / PUT / DELETE cho departments, positions, shifts, employees, attendances, locations, leave-requests (+approve/reject); đọc mở cho mọi người đăng nhập, ghi chỉ Admin (attendances: Admin/Manager sửa, leave: chủ đơn sửa/rút khi Pending). Xóa còn bị tham chiếu → 409 kèm thông báo. Admin tạo nhân viên coi liên hệ đã xác nhận để đăng nhập được; PUT employees gán phòng ban cho Manager. 85 unit test qua. Test HTTP thật bằng JWT ký tay trên SQL Server: 201/200/204/400/403/404/409 đúng, ca StartTime ≥ EndTime = 400, manager không sửa được chấm công phòng khác (404), employee1 gọi API admin = 403; đã xóa dữ liệu test |
| 15:30 | 8 | QrService (HMAC, cửa sổ hiện tại/liền trước), TrustScore (35/30/25/10, ngưỡng 70), Haversine, CheckinService: check-in/out, review, báo cáo tháng, chia sẻ vị trí; migration AddLastLocation; thêm AttendanceStatus.Rejected cho từ chối duyệt. 104 unit test qua. Test HTTP thật (JWT ký tay, SQL Server): QR employee = 403, check-in đủ điều kiện = 100đ, check-in lần 2 = 409, GPS giả = 60đ PendingReview, manager tự duyệt = 403, admin duyệt OK, duyệt lại = 409, báo cáo/share/team đúng; đã xóa dữ liệu test. Qr:Secret dev tự sinh; Geofence dev còn 0,0 - Lợi tự điền tọa độ thật trước khi demo (hoặc tạo Locations) |
| 15:50 | 8 | Rà soát thêm: giờ chấm công cố định theo giờ VN (VietnamTimeProvider, không phụ thuộc múi giờ máy chủ); chặn khởi động khi thiếu Qr:Secret (≥16 ký tự) / WindowSeconds ≤ 0; cảnh báo log khi Geofence 0,0 và chưa có Locations; khóa theo user chống bấm đúp check-in tạo 2 bản ghi; chọn ca trùng giờ theo giờ bắt đầu; báo cáo thêm RejectedCount; expiresIn QR lấy giờ 1 lần. Thêm 9.4 (faceToken) cho Bước 9. 108 unit test qua, backend khởi động OK |
