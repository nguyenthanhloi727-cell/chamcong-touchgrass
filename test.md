# Quy trình demo & quay video gửi thầy — App chấm công (nhóm Touch grass)

Mục tiêu: một video liền mạch, **~15 phút**, cho thấy toàn bộ tính năng của app điện thoại.
Tài liệu này chỉ ghi **tên tài khoản và nguồn mật khẩu**. Mật khẩu thật nằm trong `appsettings.Development.json` (không commit, không mở trong video).

## 0. Bố trí thiết bị

| Thiết bị | Vai trò | Dùng cho |
|---|---|---|
| **Emulator** (quay màn hình laptop) | Demo hầu hết tính năng, cuối cùng đóng vai Admin hiển thị mã QR | Phần 1 → 6, 8 |
| **Điện thoại thật** | Nhân viên đi chấm công: GPS thật, camera thật quét QR trên màn hình laptop | Phần 7 |

Emulator không quét được QR hiển thị trên chính nó và GPS của nó là giả (app luôn tính emulator là **GPS giả**), nên mọi lần chấm công thật phải làm trên máy thật.

## 1. Tài khoản dùng trong video

| Tài khoản | Role · Chức vụ · Phòng | Mật khẩu lấy ở đâu | Đăng nhập | Dùng ở |
|---|---|---|---|---|
| `admin` | Admin | `Admin@123` (`Seed:AdminPassword`) | **Không OTP** (không có email/SĐT đã xác nhận) | Phần 4, 6, 8 |
| `manager1` | Quản lý · Trưởng phòng Nhân sự · Phòng Hành chính | `Seed:ManagerPassword` | **OTP SMS** (cần SĐT thật, xem mục 2) | Phần 5, duyệt ở 7.2 |
| `employee1` | Nhân viên · Kế toán · Phòng Hành chính | `Seed:EmployeePassword` | OTP nếu có kênh đã xác nhận | Phần 2, 3, 7.1 |
| `nv_moi` | Nhân viên (tạo ngay trong video ở Phần 1) | tự đặt, gợi ý `Demo@12345` | OTP SMS lúc đăng ký | Phần 1, 7.2 |
| `demo01` | Nhân viên · Lễ tân (tạo ở Phần 4 rồi xóa) | `Demo@1234` | không đăng nhập | Phần 4 |

Mật khẩu `manager1`, `employee1` là mật khẩu riêng của bạn: **che đi khi quay**, đừng gõ lộ trên màn hình (dùng nút con mắt để ẩn, hoặc gõ trước khi bấm quay phần đó).

## 2. Chuẩn bị (làm trước khi bấm quay)

### 2.1 Số điện thoại: mỗi số chỉ gắn được cho một tài khoản
Backend không cho hai tài khoản dùng chung một SĐT. Hạn chế này đã gặp thật khi chuẩn bị demo: gán số đang thuộc tài khoản khác thì nút **Lưu** không báo gì và không lưu. Tài khoản chỉ có SĐT mà không có email cũng **không gỡ được SĐT** (báo "Nhập Email hoặc số điện thoại").

- Chọn **một SĐT thật** nhận được SMS, gọi là `SĐT_THẬT`.
- Tại một thời điểm chỉ một tài khoản cần OTP giữ `SĐT_THẬT`. Trước khi đăng nhập tài khoản nào bằng OTP, vào **Admin → Nhân viên → sửa** để gán `SĐT_THẬT` cho tài khoản đó (xem 2.3). Tài khoản đang giữ số cũ phải còn email thì mới gỡ được.
- Không để SĐT mẫu giả (ví dụ `0912345678`) trong `Seed:*Phone`: SMS tới số giả bị nhà mạng trả lỗi và đăng nhập kẹt ở bước OTP.
- `Seed:*` chỉ được đọc **lúc tạo user lần đầu**. Đổi file cấu hình sau đó không cập nhật user đã có, phải sửa trong app.

### 2.2 Backend và mạng
- [ ] Backend chạy, `http://localhost:5000/api/health` trả `{"status":"ok"}`.
- [ ] Mã QR đổi mỗi **60 giây** (`Qr:WindowSeconds = 60` trong `appsettings.Development.json`). Đổi giá trị này phải **khởi động lại backend**.
- [ ] Điện thoại thật và laptop **cùng Wi-Fi**, Windows Firewall mở cổng 5000; mở được `http://<IP-laptop>:5000/api/health` trên trình duyệt điện thoại (IP: `ipconfig` → IPv4 Wi-Fi).
- [ ] **SMS Gateway**: điện thoại làm gateway bật app SMS Gateway for Android, **Local server đang bật**, SIM còn tiền; `SmsGateway:BaseUrl` trong cấu hình đúng IP:cổng hiển thị trên app (hiện dạng `http://<IP>:8080`).
- [ ] Gửi thử một OTP rồi mở danh sách tin trong app gateway: trạng thái phải là **Delivered**. `Failed` kèm "Delivery result from SC … 72" nghĩa là SĐT nhận sai hoặc không tồn tại.

### 2.3 Chạy app trên 2 thiết bị (Android Studio, mở thư mục `mobile\nguyen_thanh_loi_2380601289`)

| Run Configuration | Dart entrypoint | Thiết bị |
|---|---|---|
| `emulator-admin` | `lib\main_emulator.dart` | Emulator |
| `dien-thoai-that` | `lib\main_phone.dart` (sửa `config.baseUrl` trong file này thành `http://<IP-laptop>:5000`, hiện là `192.168.1.5`) | Điện thoại thật |

Gán SĐT trong app: đăng nhập `admin` → **Nhân viên** → bấm tên tài khoản → sửa ô **Số điện thoại** → **Lưu**. Mở lại form để kiểm tra số đã đổi (số được chuẩn hóa thành `+84…`).

### 2.4 Dữ liệu cần có sẵn
- [ ] `admin`, `manager1`, `employee1` đã có (do `DbSeeder` tạo khi backend khởi động lần đầu với DB trống).
- [ ] Có 1 **Ca làm** bao trùm giờ quay (ví dụ `Ca hành chính 08:00 – 17:00`). Vào sớm tối đa 2 giờ trước giờ bắt đầu; vào sau giờ bắt đầu là **Đi muộn**. Nếu quay ngoài giờ ca, tạo ca mới ở Phần 4.
- [ ] Danh sách **Chấm công** hôm nay đang trống hoặc không có bản ghi lạ (xóa bản ghi cũ ở Admin → Chấm công).
- [ ] Phòng ban **Phòng Hành chính** có `manager1` và `employee1` (cần cho Phần 5).

### 2.5 Camera cho emulator (cho phần khuôn mặt)
Device Manager → sửa AVD → Show Advanced Settings → Camera: **Front = Webcam0** (hoặc Emulated). Chạy emulator và thử mở camera trước.

### 2.6 Điểm chấm công và vị trí
Điểm chấm công do Admin đặt bằng nút **Đặt vị trí hiện tại làm điểm chấm công** ở Màn QR. Trên emulator nút này mở **bản đồ chọn điểm**: chọn đúng chỗ bạn đang ngồi quay. Điện thoại thật dùng GPS thật nên phải ở **trong bán kính 200 m** quanh điểm đó. Chọn sai chỗ → GPS mất 45 điểm.

### 2.7 Quay
- Quay màn hình laptop (OBS hoặc `Win + G`), thu tiếng.
- Quay điện thoại thật: screen record trên điện thoại, hoặc đưa điện thoại vào khung hình khi quét QR.
- Che mật khẩu, key, chuỗi kết nối. Không mở `appsettings.Development.json` trong video. Tin SMS đưa vào khung hình chỉ nên thấy mã OTP, che SĐT nếu cần.

---

## Kịch bản quay

### Phần 1 — Đăng ký và OTP (emulator, tài khoản `nv_moi`) · ~1:30
1. Màn đăng nhập → **Chưa có tài khoản? Đăng ký**.
2. Để trống cả Email lẫn SĐT: nút xác nhận không bật và có báo lỗi.
3. Điền Họ tên, Username `nv_moi`, **Email và SĐT** (`SĐT_THẬT` nếu số đang không bị tài khoản khác giữ), Mật khẩu, Nhập lại mật khẩu → **Đăng ký** → nhập OTP nhận được trên điện thoại gateway.

**Nói:** "Đăng ký cần ít nhất Email hoặc SĐT. OTP hết hạn sau 5 phút, sai tối đa 5 lần, gửi lại cách nhau 60 giây, mỗi mã dùng một lần."

> `nv_moi` là Nhân viên. Giữ lại để dùng ở 7.2. Nên đăng ký bằng **cả Email và SĐT**: sau này gỡ SĐT khỏi tài khoản này (để gán cho người khác) mới được.

### Phần 2 — Đăng nhập có OTP (emulator, `employee1`) · ~1:00
Đăng xuất → đăng nhập `employee1` + mật khẩu → nhập OTP SMS.
**Thấy được:** nhập sai OTP một lần có báo lỗi, nhập đúng thì vào màn **Nhân viên**.
Trước phần này đảm bảo `employee1` đang giữ `SĐT_THẬT` (mục 2.1).

### Phần 3 — Hồ sơ và khuôn mặt (emulator, `employee1`) · ~2:00
1. Bấm biểu tượng **Hồ sơ**. Chỉ nhãn **Đã xác nhận / Chưa xác nhận / Chưa có** của Email và SĐT.
2. Ở kênh còn thiếu bấm thêm/xác nhận → **Gửi mã OTP** → nhập mã.
3. Ngay đầu dashboard Nhân viên có **banner cam "Đăng ký khuôn mặt"** (chỉ hiện khi chưa đăng ký) → bấm **Đăng ký ngay** để mở màn **Khuôn mặt của tôi** (hoặc vào từ **Hồ sơ**) → **Đăng ký khuôn mặt** → **chớp mắt** khi được yêu cầu → **Đã đăng ký khuôn mặt**; banner biến mất, thay bằng ô nhỏ *Khuôn mặt · Đã đăng ký · bấm để quản lý*.
4. Cũng ở màn **Khuôn mặt của tôi** (nhân viên tự quản lý): **Đăng ký lại** (ghi đè), **Thử nhận diện** (chớp mắt, báo *Khớp* / *Không khớp* kèm độ giống %), **Xóa khuôn mặt** (có hộp thoại xác nhận; xóa xong về *Chưa đăng ký*).
5. Đăng xuất → **Đăng nhập bằng khuôn mặt** → nhập username → chớp mắt → vào app.

**Nói:** "Phát hiện khuôn mặt bằng Google ML Kit, bắt buộc chớp mắt (mở – nhắm – mở) trong 5 giây để chống ảnh tĩnh. Khuôn mặt chỉ dùng để đăng nhập nhanh, không dùng trong luồng chấm công."
**Nói thẳng hạn chế:** vector khuôn mặt hiện là bản dự phòng dựng từ đường viền ML Kit, độ chính xác thấp hơn mô hình nhận dạng chuyên dụng.

### Phần 4 — Phân quyền và CRUD (emulator, `admin`) · ~3:00
Đăng nhập `admin` / `Admin@123` (vào thẳng, không OTP). Dashboard **Quản trị** là lưới 9 ô.

1. **Nhân viên → +**: tạo `demo01` / `Demo@1234`, Họ tên `Demo Test`, Email `demo01@example.com`, SĐT bất kỳ, Role **Nhân viên**, Chức vụ **Lễ tân**. Lưu → tên hiện ngay ở danh sách.
2. Bấm `demo01` để **sửa**: username bị khóa, ô mật khẩu mới để trống = giữ nguyên, SĐT được chuẩn hóa thành `+84…`. Đổi Họ tên, chọn **Phòng Hành chính** → Lưu.
3. Đổi **Role** trong form: danh sách **Chức vụ** đổi theo role.
4. Xóa `demo01` bằng biểu tượng thùng rác → hộp thoại **Xác nhận xóa** → Xóa.
5. **Phòng ban:** thêm một phòng rồi xóa. Thử xóa **Phòng Hành chính** (đang có nhân viên) → app báo *Phòng ban còn N nhân viên, hãy chuyển họ sang phòng khác trước khi xóa* (không crash).
6. **Chức vụ:** thêm một chức vụ rồi xóa.
7. **Ca làm:** nếu chưa có ca bao trùm giờ quay, tạo ca với **Giờ bắt đầu / Giờ kết thúc** phù hợp.
8. **Chấm công:** chọn ngày hoặc **Xem tất cả**, tìm kiếm theo tên, bấm một bản ghi → form **Sửa trạng thái chấm công** (đổi Ca làm, Trạng thái). Có thể bấm Hủy nếu không muốn đổi dữ liệu.
9. (Tùy chọn) Thử xóa một nhân viên **đã có bản ghi chấm công** → app báo *Nhân viên có N bản ghi chấm công, hãy khóa tài khoản thay vì xóa*.

**Nói:** "Admin không tự hạ quyền của mình và không xóa được admin cuối cùng. Quyền được đối chiếu với database ở mỗi request nên khóa tài khoản có hiệu lực ngay."

### Phần 5 — Dashboard Quản lý (emulator, `manager1`) · ~1:30
Trước phần này gán `SĐT_THẬT` cho `manager1` (mục 2.1). Đăng xuất `admin` → đăng nhập `manager1` + mật khẩu → nhập OTP.
- Dashboard **Quản lý**: 3 ô thống kê chạy số (**Lượt chấm công**, **Đi muộn**, **Chờ duyệt**), mục **Chờ duyệt**, **Đi muộn hôm nay**, **Nhân viên phòng (N)** (gồm `manager1` và `employee1`).
- **Duyệt chi tiết** → màn **Duyệt chấm công** (hiện *Không có bản ghi chờ duyệt* nếu chưa có; 7.2 sẽ tạo bản ghi).
- **Vị trí nhân viên**: chưa ai chia sẻ thì hiện *Chưa có ai chia sẻ vị trí*; 7.3 sẽ có điểm.

### Phần 6 — Admin hiển thị QR (emulator, `admin`) · ~1:00
Đăng nhập `admin` → ô **Màn QR** → **Màn QR chấm công**.
- Mã QR đổi liên tục, dòng **Đổi mã sau N giây** đếm ngược trong **60 giây** (có thể bắt đầu ở giữa chu kỳ rồi tự về 60). Quanh mã có **vòng đếm ngược** thu nhỏ dần, đổi từ xanh sang cam (≤ 15 giây) rồi đỏ (≤ 5 giây); mã mới hiện ra bằng hiệu ứng mờ-phóng nhẹ.
- Bấm **Đặt vị trí hiện tại làm điểm chấm công** → bản đồ chọn điểm (đúng chỗ bạn ngồi) → xác nhận → thông báo `Đã thêm "Điểm chấm công N" ... bán kính 200 m`.

**Giữ màn hình này mở** và phóng to cửa sổ emulator cho dễ quét.

### Phần 7 — Chấm công bằng điện thoại thật · ~4:00

#### 7.1 Chấm công hợp lệ → 100 điểm (`employee1`)
Điện thoại thật: đăng nhập `employee1` (OTP SMS hoặc khuôn mặt) → **Chấm công** → **Check-in** → app lấy vị trí → mở camera **Quét QR chấm công** → quét QR trên emulator.
**Thấy được:** GPS (cách … m) **45/45**, Mã QR **35/35**, Không dùng GPS giả **20/20**, tổng **100/100**, trạng thái **Đúng giờ** (hoặc **Đi muộn** nếu quá giờ bắt đầu ca). Quay cận cảnh 3 giây.
Quay lại dashboard Nhân viên: **Ca hôm nay**, **Điểm tin cậy lần gần nhất**, **Lịch sử chấm công tháng này**.
**Nói:** "Chấm công gồm ba bước: lấy vị trí, quét QR, gửi. GPS 45, QR 35, không GPS giả 20; từ 70 điểm trở lên là đạt."

#### 7.2 Chấm công bị nghi ngờ → chờ duyệt (`nv_moi`)
Mỗi nhân viên chỉ chấm công một lần mỗi ca nên dùng tài khoản `nv_moi`.
1. Trên emulator đang hiện QR: chụp màn hình (nút camera ở thanh công cụ emulator).
2. **Đợi hơn 60 giây** cho mã trong ảnh hết hạn.
3. Mở ảnh đó trên laptop. Điện thoại thật đăng nhập `nv_moi` → **Check-in** → quét QR **trong ảnh cũ**.

**Thấy được:** GPS 45, QR **0/35**, không GPS giả 20, tổng **65/100**, **Chờ quản lý duyệt**. Kết quả tương tự khi đứng ngoài bán kính 200 m (55 điểm) hoặc bật GPS giả (35 điểm).
**Trên emulator:** đăng nhập `manager1` → **Duyệt chấm công** → bản ghi hiện cùng **Điểm tin cậy 65** → **Duyệt** (hoặc **Từ chối**) → trạng thái đổi ngay.
**Nói:** "Quản lý chỉ duyệt được nhân viên phòng mình và không tự duyệt chấm công của chính mình."

#### 7.3 Chia sẻ vị trí, Check-out
- Điện thoại thật → **Chia sẻ vị trí hiện tại** → *Đã chia sẻ vị trí hiện tại*.
- Emulator (`manager1`) → **Vị trí nhân viên** → thấy nhân viên đó trên bản đồ/danh sách.
- Điện thoại thật → **Check-out** → *Đã check-out*.

### Phần 8 — Tổng kết (emulator, `admin`) · ~0:30
**Admin → Chấm công**: bản ghi vừa tạo xuất hiện với điểm tin cậy và trạng thái. Kết thúc video.

---

## Phần chưa có trong giao diện điện thoại (nói trước, đừng giấu)

| Nội dung | Tình trạng | Cách cho thầy thấy |
|---|---|---|
| **Báo cáo chấm công theo tháng** | Ô **Báo cáo** ở dashboard Admin hiện thông báo chưa có màn hình | Swagger → `GET /api/reports/monthly?month=2026-10` (Authorize bằng Admin) |
| **Đơn nghỉ phép** (duyệt/từ chối) | Có API, chưa có màn hình | Swagger nhóm LeaveRequests |
| **Địa điểm** (nhiều điểm chấm công) | Tạo bằng nút ở Màn QR; chưa có màn sửa/xóa | Swagger nhóm Locations |
| **Đăng nhập với Google** | Chỉ chạy trên máy có SHA-1 đã đăng ký và `Google:WebClientId` | Nói rõ không demo được trên máy này |

Các kịch bản cần phần cứng thật (SMS thật, camera, GPS ngoài vùng) chưa được kiểm thử đầy đủ trên máy thật tại thời điểm nộp, đúng như mục Hạn chế trong README.

## Kịch bản rút gọn (~7 phút)
Phần 2 (đăng nhập OTP) → Phần 3 bước 3–4 (khuôn mặt) → Phần 4 (chỉ Nhân viên + Ca làm) → Phần 6 + 7.1 (QR và 100 điểm) → Phần 7.2 (65 điểm, quản lý duyệt).

## Khắc phục sự cố nhanh

| Hiện tượng | Nguyên nhân | Cách xử lý |
|---|---|---|
| Đăng nhập báo *Không gửi được mã OTP: Delivery result … 72* | SĐT của tài khoản là số giả/không tồn tại | Sửa SĐT thành `SĐT_THẬT` (2.3) rồi đăng nhập lại |
| Bấm **Lưu** nhân viên mà form không đóng, không báo gì | SĐT đang thuộc tài khoản khác | Tìm tài khoản đang giữ số, gỡ hoặc dùng số khác |
| **Lưu** báo *Nhập Email hoặc số điện thoại* khi xóa SĐT | Tài khoản không còn email | Thêm email trước khi gỡ SĐT |
| Không xóa được nhân viên | Có bản ghi chấm công | Xóa bản ghi ở Admin → Chấm công, hoặc khóa tài khoản |
| QR vẫn đếm 30 giây | Backend chạy bản cũ | Khởi động lại backend sau khi sửa `Qr:WindowSeconds` |
| Thoát app ra màn hình chính khi đang nhập | Bấm Back lúc bàn phím không hiện | Mở lại app, đăng nhập còn nguyên |

## Kiểm nhanh trước khi nộp
- [ ] Có cảnh OTP SMS đến điện thoại thật.
- [ ] Màn kết quả chấm công hiện đủ ba dòng 45 / 35 / 20 và tổng 100.
- [ ] Có một bản ghi **Chờ quản lý duyệt** và thao tác Duyệt hoặc Từ chối của quản lý.
- [ ] Đăng ký và đăng nhập bằng khuôn mặt qua bước chớp mắt.
- [ ] Không có mật khẩu, key, chuỗi kết nối nào xuất hiện trong video.
- [ ] Đã nói rõ các phần ở bảng "chưa có trong giao diện điện thoại".
