# PLAN — App chấm công nhân viên

> Nhóm: **Touch grass** (4 người) · Cập nhật lần cuối: 22/10/2026
> Stack: **Flutter (Android) + ASP.NET Core Web API + EF Core + SQL Server**

---

## ❓ GitHub — điền trước khi push lần đầu

- [ ] **Repo push lên account nào?** Chủ repo: `__________` · Link: `https://github.com/__________/__________`
- [ ] Repo **public** (bắt buộc: thầy phải tải về và xem được), nhánh chính `main`, 3 người còn lại được add làm **Collaborator** (Settings → Collaborators)
- [ ] **Thầy clone về phải chạy được dù không có bí mật của nhóm:**
  - **Không có OTP giả.** OTP SMS luôn gửi qua server điện thoại (SMS Gateway) của Lợi. Máy nào chạy backend cũng phải cùng mạng với điện thoại đó + điền cấu hình `SmsGateway` → demo trên máy nhóm
  - Cấu hình mail thật (Gmail App Password) và gateway chỉ nằm trên máy nhóm, không commit
  - Google Sign-In không chạy trên máy thầy (thiếu SHA-1 của máy thầy) → ghi rõ trong README, demo trực tiếp trên máy nhóm
  - README ghi tài khoản demo: `admin` / `Admin@123` + 1 quản lý + 1 nhân viên

| Thành viên | Account GitHub | Đã nhận lời mời collaborator |
|---|---|---|
| Nguyễn Thành Lợi | `__________` | ⬜ |
| Hồ Ngọc Phú | `__________` | ⬜ |
| Phạm Đinh Gia Bảo | `__________` | ⬜ |
| Phương Bảo Khôi | `__________` | ⬜ |

Mỗi người cấu hình đúng tên trên máy mình trước khi commit (để lịch sử commit ghi đúng người):

```
git config --global user.name "<Họ tên>"
git config --global user.email "<email gắn với account GitHub>"
```

---

## ⚡ CHẾ ĐỘ GẤP — hạn 22:00 hôm nay (22/10), còn ~11 tiếng

**Nguyên tắc:** chạy được > đẹp. Cái gì không có trong barem thì bỏ. Mỗi giờ push 1 lần.

| Hạng mục | Làm | Cắt / làm giả |
|---|---|---|
| Đăng ký | Username + mật khẩu + **Email và/hoặc SĐT** (ít nhất 1), xác nhận bằng OTP qua kênh đã chọn; Google `idToken` | — |
| Đăng nhập | Username + mật khẩu + OTP: **SMS** nếu SĐT đã xác nhận, ngược lại **Email** | — |
| Phân quyền | Admin / Quản lý / Nhân viên + ô **Chức vụ** (dropdown 2 ô) | Không làm dashboard riêng cho từng chức vụ |
| CRUD | Nhân viên, Phòng ban, Chức vụ, Ca làm, Chấm công | Địa điểm: 1 tọa độ cố định trong `appsettings`; Đơn nghỉ phép: chỉ làm nếu dư giờ |
| Mục 5 | b. GPS geofence, e. QR (mã tĩnh ký HMAC, đổi 30s nếu kịp), a. khuôn mặt | Khuôn mặt bản tối thiểu: ML Kit phát hiện mặt + chớp mắt → mở khóa tài khoản đã lưu; so embedding nếu kịp |
| Mục 6 | Điểm tin cậy = cộng điểm GPS + QR + khuôn mặt + không GPS giả | Không ảnh, không bản đồ |
| Tool rename | **Làm** `tool/rename.dart` (mục 0) để 3 thành viên đổi tên bằng 1 lệnh | — |
| Admin | Seed `admin` / `Admin@123` (hash BCrypt), chặn tự hạ quyền | — |

### 📱 Gửi OTP qua SMS (đã test thật 22/10/2026)

#### 1. Lý do chọn giải pháp
- Đề bắt buộc: đăng nhập = username + mật khẩu + **OTP SMS gửi thật**, không được giả
- **Twilio bị loại:** gửi số VN phải đăng ký Sender ID (~5 tuần) ([Twilio VN guidelines](https://www.twilio.com/en-us/guidelines/vn/sms))
- **Chọn:** 1 điện thoại Android có SIM làm SMS gateway, app mã nguồn mở **SMS Gateway for Android** ([capcom6/android-sms-gateway](https://github.com/capcom6/android-sms-gateway)), chế độ **Local Server** → miễn phí phần mềm, chỉ tốn cước SMS của SIM, chạy trong LAN

#### 2. Kiến trúc + luồng xử lý

```
Flutter app ──HTTPS/HTTP──> Backend ASP.NET Core (laptop) ──HTTP + Basic Auth──> Điện thoại gateway (:8080, cùng LAN) ──SIM 1 Viettel──> SMS tới người dùng
```

Thiết bị gateway (đã kiểm chứng):

| Hạng mục | Giá trị |
|---|---|
| Máy | Xiaomi 11T Pro, Android 14 |
| SIM | **SIM 1 Viettel** (gửi OK). SIM 2 lỗi `RESULT_RIL_SMS_SEND_FAIL_RETRY` → **không dùng** |
| App | SMS Gateway for Android **1.77.4**, package `me.capcom.smsgateway` |
| Quyền | `SEND_SMS`, `READ_PHONE_STATE`, `POST_NOTIFICATIONS`; whitelist tối ưu pin; khóa app trong đa nhiệm |
| Địa chỉ | `http://<IP_điện_thoại>:8080` — IP do DHCP cấp, **có thể đổi** |
| Auth | HTTP Basic Auth (username/password hiển thị trong app) |

Luồng đăng nhập:
1. Flutter gửi username + mật khẩu → `POST /api/auth/login`
2. Backend kiểm tra mật khẩu → sinh OTP → lưu **hash** vào `OtpCodes` → gọi `ISmsSender.SendAsync`
3. `SmsGatewaySender` gọi gateway `POST /messages` → nhận **202 Accepted** + `id` (chỉ là **đã vào hàng đợi**, CHƯA phải đã gửi)
4. Poll `GET /messages/{id}` mỗi 2s, tối đa ~15s: `Pending → Processed → Sent/Delivered` (thành công) hoặc `Failed` (lỗi ở `recipients[].error`)
5. Thành công → trả `otpSessionId` cho Flutter; Failed / hết giờ → trả lỗi, cho phép gửi lại
6. Người dùng nhập OTP → `POST /api/auth/verify-otp` → đúng → cấp JWT, điều hướng theo role

Gọi gateway (đã chạy thành công):
```
POST /messages
Content-Type: application/json
{"textMessage":{"text":"[ChamCong] Ma OTP: 123456"},"phoneNumbers":["+84xxxxxxxxx"],"simNumber":1}
```
- `GET /health` → 200, không cần auth → dùng để kiểm tra gateway sống trước khi gửi

#### 3. Thiết kế backend

**Interface + class**
```csharp
public interface ISmsSender {
    Task<SmsResult> SendAsync(string phone, string text, CancellationToken ct);
}
```
- `SmsGatewaySender`: typed `HttpClient`, timeout 10s, Basic Auth; POST rồi poll trạng thái như bước 4; chỉ trả thành công khi `Sent`/`Delivered`
- `FakeSmsSender`: ghi log, **chỉ dùng cho dev / unit test**. Demo và bản nộp dùng `SmsGatewaySender`
- Chuẩn hóa số sang E.164: `0xxxxxxxxx` → `+84xxxxxxxxx`
- Nội dung: **không dấu, ≤160 ký tự**, có tên app `[ChamCong]`

**Cấu hình** (`appsettings.Development.json` / user-secrets — **KHÔNG commit GitHub**)

| Key | Giá trị |
|---|---|
| `SmsGateway:BaseUrl` | `http://<IP_điện_thoại>:8080` |
| `SmsGateway:Username` | lấy trong app |
| `SmsGateway:Password` | lấy trong app |
| `SmsGateway:SimNumber` | `1` |
| `SmsGateway:TimeoutSeconds` | chưa chốt (poll tối đa ~15s) |

**Bảng `OtpCodes`**

| Cột | Ghi chú |
|---|---|
| `Id` | khóa chính |
| `UserId` | FK → Users |
| `Channel` | Email / Sms |
| `Destination` | email hoặc SĐT (E.164) nhận mã |
| `CodeHash` | hash của OTP, không lưu OTP gốc |
| `Purpose` | Register, Login, AddContact |
| `ExpiresAt` | `CreatedAt + 5 phút` |
| `Attempts` | số lần nhập sai |
| `IsUsed` | OTP dùng 1 lần |
| `GatewayMessageId` | `id` gateway trả về, để tra trạng thái |
| `CreatedAt` | dùng tính giới hạn gửi lại |

**API**

| Endpoint | Việc |
|---|---|
| `POST /api/auth/register` | `{username, password, email?, phone?, verifyChannel: "email"\|"sms"}` → gửi OTP → `otpSessionId` |
| `POST /api/auth/login` | user + pass đúng → gửi OTP (SMS nếu SĐT đã xác nhận, không thì Email) → `otpSessionId` |
| `POST /api/auth/verify-otp` | `otpSessionId` + mã → đăng ký: kích hoạt tài khoản; đăng nhập: trả JWT |
| `POST /api/auth/resend-otp` | gửi lại, cách lần trước ≥60s |
| `POST /api/me/contact` + `/api/me/contact/verify` | thêm / xác nhận Email hoặc SĐT còn thiếu |

#### 4. Bảo mật OTP
- Sinh mã: `RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6")` — **không dùng `System.Random`**
- Chỉ lưu **hash** OTP trong DB
- Hết hạn **5 phút**; sai tối đa **5 lần** thì hủy OTP; **dùng 1 lần**; gửi lại cách nhau **≥60 giây**
- Username/password gateway không nằm trong source; đổi mật khẩu gateway trước demo

#### 5. Rủi ro + phương án dự phòng

| Rủi ro | Phòng / xử lý |
|---|---|
| Điện thoại đổi IP, mất Wi-Fi, Wi-Fi trường chặn client-to-client | Kiểm tra IP + `GET /health` trước demo; dự phòng: **hotspot laptop** cho điện thoại kết nối |
| Xiaomi kill app chạy nền | Đã whitelist pin + khóa đa nhiệm; kiểm tra Service đang chạy trước demo |
| SIM hết tiền / lỗi sóng | Nạp tiền trước; **gửi thử 1 tin trước buổi demo** |
| 202 nhưng thực tế Failed (đã gặp với SIM 2) | Luôn poll trạng thái, chỉ tin `Sent`/`Delivered`; cố định `simNumber: 1` |
| Lộ username/password gateway | Không đưa vào source; đổi mật khẩu trước demo |
| Chỉ 1 thiết bị gateway | **Điểm lỗi duy nhất** — ghi nhận là giới hạn của giải pháp |

#### 6. Phân công + mốc thời gian

| Việc | Người | Mốc |
|---|---|---|
| Dựng gateway trên Xiaomi 11T Pro, test gửi thật | Phú | ✅ xong 22/10 |
| `ISmsSender`, `SmsGatewaySender`, `FakeSmsSender`, bảng `OtpCodes`, 3 API auth | Lợi | 12:00–15:00 |
| Màn nhập OTP + nút gửi lại (đếm ngược 60s) trên Flutter | _(chưa phân)_ | 12:00–15:00 |
| Test luồng login + OTP SMS thật cả nhóm | Cả nhóm | Merge #1 15:00–15:30 |
| Kiểm tra trước demo: IP, `/health`, Service chạy, SIM còn tiền, gửi thử 1 tin | _(chưa phân)_ | 21:30–22:00 |

#### 7. Tiêu chí nghiệm thu
- [ ] Đăng nhập đúng user + pass → điện thoại người dùng **nhận SMS thật** chứa OTP 6 số
- [ ] Backend chỉ báo thành công khi gateway trả `Sent`/`Delivered`; `Failed` hoặc quá ~15s → app hiện lỗi
- [ ] Nhập đúng OTP → vào đúng màn hình theo role; nhập sai 5 lần → OTP bị hủy
- [ ] OTP quá 5 phút → bị từ chối; OTP đã dùng → không dùng lại được
- [ ] Gửi lại trước 60s → bị từ chối
- [ ] DB chỉ có `CodeHash`, không có OTP gốc
- [ ] Repo GitHub không chứa `BaseUrl`/`Username`/`Password` của gateway
- [ ] Demo trên máy nhóm, SMS gửi thật qua server điện thoại của Lợi (không dùng `FakeSmsSender` khi demo / nộp)

---

## 0. Luật bắt buộc (vi phạm = 0đ / rớt môn)

- [ ] **"2 source code giống nhau" = giữa 2 NHÓM.** Trong nhóm được dùng chung source, nhưng **mỗi người phải đổi tên project/file theo tên mình**
  - Flutter: `flutter create <ho_va_ten_mssv>` (vd `nguyen_thanh_loi_2380601289`), label app = "Họ Tên - MSSV", đổi `applicationId` (vd `com.touchgrass.nguyen_thanh_loi_2380601289`)
  - Backend: `<HoVaTen_MSSV>.ChamCong.Api` (vd `NguyenThanhLoi_2380601289.ChamCong.Api`)
  - MSSV luôn đặt **sau** tên: Dart package và namespace C# không được bắt đầu bằng số
  - Không copy code từ nhóm khác
- [ ] **Repo PUBLIC** → 2 việc bắt buộc:
  - **Không commit bí mật:** `appsettings.Development.json`, `.env` vào `.gitignore`; chỉ commit `appsettings.example.json` / `.env.example` (giá trị rỗng). Bí mật gồm: Gmail App Password, JWT secret, chuỗi kết nối SQL, Google client secret
  - **Chống bị nhóm khác chép:** push thường xuyên (mỗi giờ) để lịch sử commit chứng minh nhóm mình làm trước. Không gửi link repo ra ngoài nhóm trước 22:00
  - `admin` / `Admin@123` được phép ghi trong README (tài khoản demo cho thầy), nhưng chỉ lưu hash trong DB
- [ ] **Bắt buộc push GitHub + test sau mỗi lần làm:** build/chạy được → push thẳng vào `main`

### Tên project từng thành viên

| Thành viên | MSSV | Flutter | Backend |
|---|---|---|---|
| Nguyễn Thành Lợi | 2380601289 | `nguyen_thanh_loi_2380601289` | `NguyenThanhLoi_2380601289.ChamCong.Api` |
| Hồ Ngọc Phú | 2380601699 | `ho_ngoc_phu_2380601699` | `HoNgocPhu_2380601699.ChamCong.Api` |
| Phạm Đinh Gia Bảo | 2380600166 | `pham_dinh_gia_bao_2380600166` | `PhamDinhGiaBao_2380600166.ChamCong.Api` |
| Phương Bảo Khôi | 2380601104 | `phuong_bao_khoi_2380601104` | `PhuongBaoKhoi_2380601104.ChamCong.Api` |

### Tool đổi tên `tool/rename.dart`

Mỗi người clone về chạy 1 lệnh để ra bản mang tên mình, không sửa tay:

```
dart run tool/rename.dart --name "Nguyễn Thành Lợi" --mssv 2380601289
```

Script tự bỏ dấu (`đ` → `d`) → `nguyen_thanh_loi_2380601289` / `NguyenThanhLoi_2380601289`, rồi đổi:

| Phần | Chỗ đổi |
|---|---|
| Flutter | `name:` trong `pubspec.yaml`, import `package:<tên cũ>/` (nếu còn) |
| Android | `applicationId` + `namespace` trong `build.gradle`, `android:label` trong `AndroidManifest.xml`, thư mục + dòng `package` của `MainActivity.kt` |
| Thư mục | `mobile/<ten_cu>/` → `mobile/<ten_moi>/` |
| Backend | tên `.csproj` + thư mục, đường dẫn trong `.sln`, `namespace`/`using` trong mọi file `.cs`, `RootNamespace` |

Quy tắc dùng:
- `main` giữ tên **Nguyễn Thành Lợi** làm gốc. Người khác chỉ chạy script trên bản của mình, **không push bản đã đổi tên lên `main`**
- Import trong Dart dùng **đường dẫn tương đối** (`../services/x.dart`) để đổi tên không hỏng import
- Mỗi `applicationId` cần **1 Android OAuth client riêng** trên Google Cloud Console (package name + SHA-1 của người đó) → tổng 4 client
- Chạy script xong phải test: `flutter clean` → `flutter pub get` → `flutter run`, và `dotnet build`

---

## 1. Đề tài

**App chấm công cho nhân viên**, chọn vì hợp barem nhất:

- Phân quyền tự nhiên: Admin / Quản lý / Nhân viên
- CRUD nhiều bảng, nghiệp vụ rõ
- Face + GPS + QR dùng thẳng vào việc chấm công

---

## 2. Công nghệ

| Tầng | Công nghệ |
|---|---|
| Mobile | Flutter: `google_sign_in`, `geolocator`, `mobile_scanner`, `google_mlkit_face_detection`, `tflite_flutter` (MobileFaceNet) |
| Backend | ASP.NET Core Web API (.NET 8 hoặc 10 LTS, khóa bằng `global.json`) |
| ORM | EF Core + SQL Server, tạo bảng bằng migration |
| Database | SQL Server Developer/Express, quản lý bằng SSMS |
| Auth | JWT (access 15', refresh 7 ngày), Google OAuth 2.0 (`Google.Apis.Auth`), OTP Email (MailKit), OTP SMS (SMS Gateway for Android, local), BCrypt |
| API docs | Swagger (`Swashbuckle.AspNetCore` nếu template không có sẵn) |

---

## 3. Chức năng theo barem

### Mục 1 — Đăng ký

**Form đăng ký:** Username, Mật khẩu, ô **Email** và ô **Số điện thoại** — được bỏ trống 1 trong 2, **không được trống cả 2**.

| Nhập | Nút xác nhận | OTP gửi tới |
|---|---|---|
| Chỉ Email | "Xác nhận qua Email" | Email |
| Chỉ SĐT | "Xác nhận qua SMS" | SMS (server điện thoại) |
| Cả 2 | Chọn 1 trong 2 nút | Kênh đã chọn |

Luồng:
1. Bấm nút xác nhận → backend kiểm tra hợp lệ → tạo user `Pending` → gửi OTP qua kênh đã chọn → trả `otpSessionId`
2. Nhập OTP vào **1 ô duy nhất** → đúng → user `Active`, đánh dấu kênh đó đã xác nhận (`EmailVerified` hoặc `PhoneVerified`)
3. Kênh còn lại (nếu có nhập) để **chưa xác nhận**; bổ sung / xác nhận sau trong màn **Hồ sơ** (mỗi lần thêm kênh mới phải nhập OTP gửi tới chính kênh đó)

- **Google (OAuth 2.0):** app lấy `idToken` → backend xác thực bằng `GoogleJsonWebSignature.ValidateAsync` → tạo/tìm user, `EmailVerified = true` → cấp JWT

### Mục 2 — Đăng nhập
1. Username + mật khẩu
2. Backend gửi OTP: **SMS** (server điện thoại) nếu `PhoneVerified`, nếu không thì **Email** → nhập mã → nhận JWT
- OTP lưu hash, hết hạn 5 phút, sai 5 lần hủy, gửi lại cách ≥60s (chi tiết: mục "Gửi OTP qua SMS")

### Mục 3 — Phân quyền

| Role | Sau đăng nhập |
|---|---|
| Admin | Trang quản trị: nhân viên, phòng ban, ca, địa điểm, báo cáo |
| Quản lý | Dashboard phòng ban: đi muộn / vắng, duyệt đơn |
| Nhân viên | Dashboard cá nhân: chấm công, lịch ca, công tháng, gửi đơn |

Backend chặn quyền ở từng API: `[Authorize(Roles = "...")]`.

**Phân quyền 2 ô** (màn chỉnh role của admin):

| Ô 1 — Role | Ô 2 — Chức vụ (lọc theo ô 1) |
|---|---|
| Quản lý | Trưởng phòng Nhân sự, Quản lý Kho, Quản lý Tài chính |
| Nhân viên | Kế toán, Thủ kho, Lễ tân, Bảo vệ, Lao công |
| Admin | _(không có chức vụ)_ — tài khoản `admin` / `Admin@123`, không tự hạ quyền, không xóa được admin cuối cùng |

### Mục 4 — CRUD (4.5đ)

Làm chắc:
1. Nhân viên (có dropdown Role + Chức vụ, số điện thoại)
2. Phòng ban
3. Chức vụ
4. Ca làm
5. Chấm công

Nếu dư giờ:
6. Địa điểm chấm công (tọa độ + bán kính) — chưa làm thì dùng 1 tọa độ cố định trong `appsettings`
7. Đơn nghỉ phép / giải trình (duyệt / từ chối)

### Mục 5 — 3 chức năng nâng cao (3.5đ)

| Chức năng | Cách làm |
|---|---|
| a. Đăng nhập khuôn mặt | ML Kit phát hiện mặt + kiểm tra chớp mắt; MobileFaceNet tạo embedding; backend so cosine similarity |
| b. Chia sẻ vị trí | Check-in chỉ trong bán kính 100m, chặn GPS giả (`isMocked`); nhân viên thị trường chia sẻ vị trí cho quản lý |
| e. QR | Máy lễ tân hiện QR đổi mỗi 30s (ký HMAC), nhân viên quét mới chấm được |

Dự phòng: vân tay (`local_auth`) hoặc NFC.

### Mục 6 — Tính năng mới (1đ): Điểm tin cậy chấm công
- Mỗi lần chấm công có điểm 0–100 từ: khuôn mặt, GPS, QR, GPS giả, thiết bị đã đăng ký
- Điểm thấp → tự chuyển quản lý duyệt kèm ảnh + vị trí
- [ ] Kiểm tra Base Checkin, MISA AMIS, 1Office xem đã có tính năng này chưa

### Mục 7 — Giao diện, cả nhóm chạy được (1đ)
- Material 3
- README chạy từng bước
- Seed sẵn 3 tài khoản: admin / manager / employee

---

## 4. Kiến trúc

```
backend/<HoVaTen_MSSV>.ChamCong.Api/   Controllers, Services, Data (DbContext, Migrations), DTOs, Tests
mobile/<ho_va_ten_mssv>/               Flutter app
README.md
```

Code chung nằm ở repo nhóm (**public**), push thẳng `main`. Bản nộp của từng người = cùng code, đổi tên bằng `tool/rename.dart` theo bảng ở mục 0.

| Nhóm | Endpoint |
|---|---|
| Auth | `/api/auth/register`, `/api/auth/verify-email`, `/api/auth/login`, `/api/auth/verify-otp`, `/api/auth/resend-otp`, `/api/auth/google`, `/api/auth/refresh` |
| CRUD | `/api/employees`, `/api/departments`, `/api/positions`, `/api/shifts`, `/api/attendances` (+ `/api/locations`, `/api/leave-requests` nếu dư giờ) |
| Chấm công | `/api/attendance/check-in`, `/api/qr/current`, `/api/face/enroll`, `/api/face/login`, `/api/reports/monthly` |

### Thiết kế CSDL (SQL Server, tạo bằng EF migration)

| Bảng | Cột chính | Ghi chú |
|---|---|---|
| `Users` | `Id`, `Username` (unique), `Email` (null), `EmailVerified`, `PhoneNumber` (E.164, null), `PhoneVerified`, `PasswordHash`, `FullName`, `Role` (Admin/Manager/Employee), `PositionId` (FK, null), `DepartmentId` (FK, null), `Status` (Pending/Active/Locked), `GoogleSub` (null), `FaceEmbedding` (NVARCHAR(MAX), null), `CreatedAt` | CHECK `Email IS NOT NULL OR PhoneNumber IS NOT NULL`. Unique `Email`, `PhoneNumber` phải là **filtered index** (`HasFilter("[Email] IS NOT NULL")`) — SQL Server coi NULL là trùng nhau, không lọc thì chỉ 1 user được để trống. `admin` seed có SĐT đã xác nhận |
| `Departments` | `Id`, `Name`, `ManagerId` (FK → Users, null) | |
| `Positions` | `Id`, `Name`, `RoleGroup` (Manager/Employee) | Backend chặn `Position.RoleGroup ≠ User.Role` |
| `Shifts` | `Id`, `Name`, `StartTime`, `EndTime` | |
| `Attendances` | `Id`, `UserId` (FK), `ShiftId` (FK), `CheckInAt`, `CheckOutAt`, `Lat`, `Lng`, `IsMocked`, `QrValid`, `FaceMatched`, `TrustScore` (0–100), `Status` (OnTime/Late/PendingReview) | `TrustScore` thấp → `PendingReview` (mục 6) |
| `OtpCodes` | xem mục "Gửi OTP qua SMS" | `Channel` Email/Sms; `Purpose` Register/Login/AddContact |
| `Locations` | `Id`, `Name`, `Lat`, `Lng`, `RadiusMeters` | Nếu dư giờ |
| `LeaveRequests` | `Id`, `UserId`, `FromDate`, `ToDate`, `Reason`, `Status`, `ApprovedBy` | Nếu dư giờ |

Seed: `admin` + 1 quản lý + 1 nhân viên, toàn bộ `Positions` ở bảng phân quyền 2 ô, 1 phòng ban, 1 ca.

---

## 5. Phân công

> **Dev chính: Nguyễn Thành Lợi** — làm toàn bộ code. Bảng dưới là phân công trên danh nghĩa.

| Thành viên | Phụ trách |
|---|---|
| TV1 — Nguyễn Thành Lợi (lead) | Backend auth (đăng ký, Google OAuth, login + OTP, JWT, phân quyền), setup DB, gộp code, tool `rename.dart` |
| TV2 — Hồ Ngọc Phú | Khuôn mặt: app (ML Kit, MobileFaceNet) + API enroll/login |
| TV3 — Phạm Đinh Gia Bảo | Chấm công: GPS, QR động, điểm tin cậy, màn hình check-in |
| TV4 — Phương Bảo Khôi | CRUD (API + UI), dashboard, README, slide |

---

## 6. Tiến độ

| Giờ | Lợi | Phú | Bảo | Khôi | Trạng thái |
|---|---|---|---|---|---|
| 11:00–12:00 | Tạo repo, project .NET + Flutter, EF models, migration, seed admin + chức vụ → **push** | Cài SQL Server, bật SQL Auth + TCP/IP; lấy SHA-1; SMS gateway ✅ đã xong | Tạo Google Cloud project, OAuth client Android + Web | Dựng theme, màn đăng nhập/đăng ký (UI tĩnh) | ⬜ |
| 12:00–15:00 | API auth: register, OTP email + **OTP SMS (gọi gateway)**, login + OTP, Google, JWT, phân quyền | Màn khuôn mặt: ML Kit + chớp mắt | API + màn chấm công: GPS, QR, check-in | Điều hướng theo role, 3 dashboard khung, CRUD Phòng ban + Chức vụ | ⬜ |
| 15:00–15:30 | **Merge #1 + test cả nhóm** | | | | ⬜ |
| 15:30–18:30 | API CRUD còn lại, chặn quyền admin | Nối khuôn mặt vào login + check-in | Điểm tin cậy (mục 6), lịch sử chấm công | CRUD Nhân viên (dropdown Role + Chức vụ), Ca làm | ⬜ |
| 18:30–19:00 | **Merge #2 + test cả nhóm** | | | | ⬜ |
| 19:00–20:30 | Sửa lỗi, test toàn luồng theo 3 role | Sửa lỗi | Sửa lỗi | Làm đẹp UI, README | ⬜ |
| 20:30–21:30 | **Mỗi người đổi tên project theo mục 0 → build → chạy trên máy mình → push** | | | | ⬜ |
| 21:30–22:00 | Dự phòng, kiểm tra repo, **nộp** | | | | ⬜ |

**Quy tắc gấp:** quá 1 tiếng kẹt 1 lỗi → báo nhóm, chuyển sang phương án cắt ở bảng "Chế độ gấp".

---

## 7. Rủi ro

| Rủi ro | Cách xử lý |
|---|---|
| Google Sign-In lỗi trên máy thành viên | Thêm SHA-1 debug key của cả 4 máy vào Google Cloud Console |
| Điện thoại không gọi được API | `dotnet run --urls http://0.0.0.0:5000`, mở cổng Firewall; bản debug bật `usesCleartextTraffic` |
| Backend không kết nối SQL Server | Bật SQL Authentication + TCP/IP cổng 1433 |
| DB mỗi máy một kiểu | Chỉ dùng EF migration, không tạo bảng tay trong SSMS |
| Khuôn mặt không kịp | Chuyển sang vân tay (`local_auth`) |
| SMS gateway không gửi được | Xem bảng rủi ro trong mục "Gửi OTP qua SMS" |
| Lộ bí mật trên repo public | `.gitignore` từ commit đầu tiên; lỡ push App Password → thu hồi ngay trong Google Account và tạo cái mới (xóa commit không đủ, lịch sử vẫn còn) |
| Đổi tên sót file → build lỗi hoặc còn tên người khác | Dùng `tool/rename.dart`, sau đó `grep` tên cũ trong repo phải ra 0 kết quả |
| Thành viên dùng Mac | Code bằng VS Code + C# Dev Kit, trỏ DB vào laptop Windows trong nhóm |

---

## 8. Câu hỏi còn mở

- [x] ~~Nộp 1 repo nhóm hay mỗi người 1 bản?~~ → trong nhóm dùng chung source, mỗi người đổi tên theo tên mình
- [x] ~~Hạn nộp?~~ → 22:00 ngày 22/10/2026
- [x] ~~Mục 5 chọn chức năng nào?~~ → a, b, e (3 chức năng còn lại không liên quan chấm công)
- [ ] Mỗi người nộp repo riêng hay chung 1 repo nhiều folder?

---

## Changelog

| Ngày | Thay đổi |
|---|---|
| 22/10/2026 | Tạo plan: chốt đề chấm công, stack Flutter + ASP.NET Core + SQL Server |
| 22/10/2026 | Mục 0: "source giống nhau" là giữa 2 nhóm; mỗi người đổi tên project; bắt buộc push GitHub + test sau mỗi lần làm; thêm quy trình git |
| 22/10/2026 | Tên project thêm MSSV: `ho_va_ten_mssv` (Flutter), `HoVaTen_MSSV` (backend) |
| 22/10/2026 | Thêm tool `tool/rename.dart` (mục 0), giao TV1 làm tuần 1, thêm rủi ro đổi tên sót |
| 22/10/2026 | Điền tên + MSSV + tên project của Phú, Bảo, Khôi; gán TV2–TV4 trong phân công |
| 22/10/2026 | Chuyển sang chế độ gấp: hạn 22:00 hôm nay; cắt phạm vi; thêm Chức vụ (2 ô Role + Chức vụ) và seed admin; tiến độ theo giờ |
| 22/10/2026 | Repo public: thêm quy tắc không commit bí mật, chống bị chép, rủi ro lộ key |
| 22/10/2026 | Thêm mục GitHub đầu file: account chủ repo, account từng thành viên, collaborator, git config |
| 22/10/2026 | Ghi lý do repo public (thầy tải về); thêm `Otp:DevMode` để thầy chạy được không cần bí mật |
| 22/10/2026 | SMS OTP phải thật: bỏ Twilio (không gửi được số VN), dùng SMS Gateway for Android chạy local; Phú dựng gateway 11:00–12:00, Lợi nối API |
| 22/10/2026 | Viết đầy đủ mục "Gửi OTP qua SMS" theo kết quả test thật (Xiaomi 11T Pro, app 1.77.4, SIM 1 Viettel, `POST /messages` + poll trạng thái); đổi API auth sang `/api/auth/*` |
| 22/10/2026 | Rà soát theo đề: thêm thiết kế CSDL; login chỉ OTP SMS qua server điện thoại, bỏ OTP giả cho máy thầy; thống nhất danh sách CRUD + phân quyền 2 ô; repo public, push thẳng `main`, bỏ quy trình PR; làm `tool/rename.dart`; Lợi là dev chính; dọn câu hỏi mở |
| 22/10/2026 | Đăng ký: Email và/hoặc SĐT (ít nhất 1), OTP 1 ô qua kênh đã chọn, kênh còn lại bổ sung sau; login OTP SMS nếu SĐT đã xác nhận, không thì Email; `OtpCodes` thêm `Channel`/`Destination`; filtered unique index |
