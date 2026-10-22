# App chấm công nhân viên — nhóm Touch grass

| Thành viên | MSSV |
|---|---|
| Nguyễn Thành Lợi | 2380601289 |
| Hồ Ngọc Phú | 2380601699 |
| Phạm Đinh Gia Bảo | 2380600166 |
| Phương Bảo Khôi | 2380601104 |

**Stack:** Flutter (Android) · ASP.NET Core Web API (.NET 10) · EF Core 10 · SQL Server

## Kiến trúc

```
Flutter app ──HTTP──> Backend ASP.NET Core (laptop) ──HTTP + Basic Auth──> Điện thoại SMS Gateway (:8080, cùng LAN) ──SIM──> SMS tới người dùng
                              │
                              ├── SQL Server (EF Core)
                              └── SMTP Gmail (OTP qua email)
```

```
backend/NguyenThanhLoi_2380601289.ChamCong.Api/         Web API
backend/NguyenThanhLoi_2380601289.ChamCong.Api.Tests/   Unit test (xUnit)
mobile/nguyen_thanh_loi_2380601289/                     Flutter app
tool/rename.dart                                        Đổi tên project theo họ tên + MSSV
```

## Lưu ý quan trọng khi chạy

- **SMS OTP được gửi thật** qua điện thoại Android có SIM của nhóm (app mã nguồn mở [SMS Gateway for Android](https://github.com/capcom6/android-sms-gateway), chế độ Local Server). Không có OTP giả. Backend phải cùng mạng với điện thoại đó và có cấu hình `SmsGateway` → **luồng SMS chỉ demo được trên máy nhóm**.
- OTP qua email dùng Gmail App Password của nhóm (không commit) → cũng chỉ chạy được khi có cấu hình riêng.
- **Google Sign-In** cần SHA-1 của máy build được đăng ký trong Google Cloud và `Google:WebClientId` → không chạy trên máy khác; demo trực tiếp trên máy nhóm.

## Cài đặt

### 1. Yêu cầu
.NET SDK 10 (khóa trong `backend/global.json`), Flutter 3.x, SQL Server (Express được), `dotnet-ef` (đã khai báo trong `backend/.config/dotnet-tools.json`, cài bằng `dotnet tool restore` trong thư mục `backend`), điện thoại Android.

### 2. SQL Server
Bật **SQL Server Authentication** (Mixed Mode) và **TCP/IP** (SQL Server Configuration Manager → Protocols → TCP/IP = Enabled, cổng 1433), khởi động lại dịch vụ. Dùng Windows Authentication trên cùng máy cũng được.

### 3. Backend

```bash
cd backend/NguyenThanhLoi_2380601289.ChamCong.Api
copy appsettings.example.json appsettings.Development.json
```

Mở `appsettings.Development.json` và điền (file này **không được commit**):

| Key | Ý nghĩa |
|---|---|
| `ConnectionStrings:Default` | Chuỗi kết nối SQL Server |
| `Jwt:Key` | Khóa ký JWT (≥ 32 ký tự ngẫu nhiên) |
| `Otp:HashPepper` | Chuỗi bí mật để băm OTP |
| `Email:*` | SMTP Gmail + App Password |
| `SmsGateway:*` | `BaseUrl`, `Username`, `Password` của điện thoại gateway |
| `Google:WebClientId` | OAuth Web Client ID (nếu dùng Google Sign-In) |
| `Qr:Secret` | Khóa HMAC cho mã QR (≥ 16 ký tự) |
| `Geofence:*` | Tọa độ + bán kính mặc định (hoặc tạo Địa điểm trong app) |
| `Seed:*Password`, `Seed:*Email`, `Seed:*Phone` | Mật khẩu / liên hệ của 3 tài khoản seed |

Tạo DB và chạy (từ thư mục `backend`):

```bash
dotnet tool restore
dotnet restore
dotnet ef database update --project NguyenThanhLoi_2380601289.ChamCong.Api
dotnet run --project NguyenThanhLoi_2380601289.ChamCong.Api --urls http://0.0.0.0:5000
```

`ef` chỉ cần `ConnectionStrings:Default` (mặc định SQL Server LocalDB trong `appsettings.json`; dùng SQL Server khác thì ghi đè trong `appsettings.Development.json`), không cần điền Jwt/Qr/Seed. Trong Visual Studio Package Manager Console: chọn **Default project** và **Startup project** đều là project Api rồi chạy `update-database`; thêm migration mới bằng `add-migration <Ten>`. Repo đã có sẵn migration, không cần tạo `InitialDatabase`.

Swagger: `http://localhost:5000/swagger` · Kiểm tra sống: `http://<IP-laptop>:5000/api/health`. Mở cổng 5000 trên Windows Firewall để điện thoại truy cập được. Test: `dotnet test backend/ChamCong.slnx`.

### 4. Flutter

```bash
cd mobile/nguyen_thanh_loi_2380601289
flutter pub get
flutter run --dart-define=BASE_URL=http://<IP-laptop>:5000
```

Điện thoại và laptop phải cùng mạng Wi-Fi. Nếu build Android lỗi `Unable to establish loopback connection` do đường dẫn TEMP có dấu cách, đặt `TEMP` và `TMP` về thư mục không dấu cách (vd. `C:\tmp`) trước khi build.

### 5. Đổi tên theo thành viên
Chạy ở gốc repo, trên bản copy riêng (không push lên `main`):

```bash
dart run tool/rename.dart --name "Hồ Ngọc Phú" --mssv 2380601699
```

Script đổi namespace, tên project, package Flutter, `applicationId`, nhãn app; dùng `--dry-run` để xem trước. Muốn đăng nhập Google trên bản đã đổi tên cần đăng ký thêm SHA-1 + OAuth client Android cho `applicationId` mới.

## Tài khoản demo

| Tài khoản | Mật khẩu | Vai trò |
|---|---|---|
| `admin` | `Admin@123` | Admin |
| `manager1` | đặt tại `Seed:ManagerPassword` | Quản lý (phòng ban mẫu) |
| `employee1` | đặt tại `Seed:EmployeePassword` | Nhân viên (phòng ban mẫu) |

Tài khoản được tạo bởi `DbSeeder` khi backend khởi động (mật khẩu băm BCrypt, chỉ lưu hash). Chỉ tạo khi `Seed:*Password` tương ứng không rỗng.

## Chức năng (theo 7 mục đề)

1. **Đăng ký** — username + mật khẩu + Email và/hoặc SĐT (ít nhất 1), xác nhận bằng OTP qua kênh đã chọn; đăng ký bằng Google (`idToken`).
2. **Đăng nhập** — username + mật khẩu + OTP (SMS nếu SĐT đã xác nhận, ngược lại Email); OTP hết hạn 5 phút, sai tối đa 5 lần, gửi lại cách nhau 60 giây, mỗi mã dùng một lần, DB chỉ lưu `CodeHash`; JWT + refresh token.
3. **Phân quyền** — Admin / Quản lý / Nhân viên + Chức vụ (dropdown 2 ô lọc theo vai trò); Admin không tự hạ quyền, không xóa được admin cuối cùng; role/trạng thái được đối chiếu với DB ở mỗi request.
4. **CRUD** — Nhân viên, Phòng ban, Chức vụ, Ca làm, Chấm công (+ Địa điểm, Đơn nghỉ phép có duyệt/từ chối); xóa bản ghi còn bị tham chiếu trả 409.
5. **Phương thức chấm công**
   - a. Khuôn mặt: ML Kit phát hiện mặt, bắt buộc chớp mắt chống ảnh tĩnh; đăng ký mặt, đăng nhập bằng mặt, xác minh khi chấm công (`faceToken` ngắn hạn do server cấp).
   - b. GPS geofence (Haversine), phát hiện GPS giả, chia sẻ vị trí cho quản lý xem.
   - e. QR động: mã ký HMAC, đổi mỗi 30 giây, Admin hiển thị tại điểm chấm công; quét mã cũ → không hợp lệ.
6. **Tính năng mới: Điểm tin cậy** — mỗi lần chấm công tính điểm: GPS trong vùng 45 + QR hợp lệ 35 + không GPS giả 20. Dưới ngưỡng 70 → `PendingReview`, quản lý duyệt hoặc từ chối.
7. **Hồ sơ & báo cáo** — hồ sơ hiển thị Email/SĐT với nhãn Đã xác nhận / Chưa xác nhận / Chưa có, bổ sung và xác nhận liên hệ bằng OTP; dashboard riêng cho từng vai trò; báo cáo chấm công theo tháng.

## Khuôn mặt (đăng ký, đăng nhập, chấm công)

- Phát hiện mặt + xác suất mở mắt: Google ML Kit Face Detection (`google_mlkit_face_detection`). Bắt buộc chớp mắt (mở → nhắm → mở) trong 5 giây để chống ảnh tĩnh.
- **Embedding hiện là bản dự phòng, không phải MobileFaceNet:** vector hình dạng từ các đường viền ML Kit (mặt, lông mày, mắt, môi, mũi), xoay theo góc nghiêng đầu, chuẩn hóa tâm/kích thước/độ dài 1. Độ chính xác thấp hơn MobileFaceNet; chưa có file model `.tflite` nào trong repo (nguồn và license chưa chốt).
- Backend so cosine similarity với ngưỡng `Face:Threshold` (`appsettings`); cần chỉnh theo thử nghiệm thật vì embedding dự phòng cho điểm tương đồng cao giữa các khuôn mặt.

## Hạn chế

- Test tự động: 115 test backend và 16 test Flutter qua, `flutter analyze` sạch. Các kịch bản cần điện thoại thật (nhận SMS/mail thật, Google Sign-In, chụp mặt, quét QR, GPS ngoài vùng) chưa được kiểm thử đầy đủ trên máy thật tại thời điểm nộp.
- Khuôn mặt dùng embedding dự phòng từ ML Kit (xem trên), độ chính xác hạn chế, không phải mô hình nhận dạng khuôn mặt chuyên dụng.
- SMS OTP phụ thuộc điện thoại gateway (SIM 1 còn tiền, cùng LAN, IP DHCP có thể đổi); Google Sign-In chỉ chạy với SHA-1 đã đăng ký.
- Chỉ hỗ trợ Android; không có ảnh chấm công, không có bản đồ.
- Đăng ký chỉ bằng SĐT chỉ được kiểm bằng unit test (SĐT của nhóm đã thuộc tài khoản admin).
