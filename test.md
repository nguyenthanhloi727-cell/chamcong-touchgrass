# Hướng dẫn demo & quay video gửi thầy — App chấm công (nhóm Touch grass)

Mục tiêu: một video liền mạch, **~15 phút**, cho thấy toàn bộ tính năng của app điện thoại.

**Cách bố trí thiết bị**

| Thiết bị | Vai trò | Dùng để |
|---|---|---|
| **Emulator** (máy chính, quay màn hình laptop) | Demo hầu hết tính năng, sau đó **đóng vai Admin** hiển thị mã QR | Phần 1 → 6, 8 |
| **Điện thoại thật** (1 chiếc) | Nhân viên đi chấm công: có GPS thật, camera thật quét được QR trên màn hình laptop | Phần 7 |

Vì sao cần máy thật: emulator không quét được QR hiển thị trên chính màn hình của nó và GPS của nó là giả. App cũng nhận ra điều đó: **emulator luôn bị tính là GPS giả**, nên mọi lần chấm công thật phải làm trên máy thật.

---

## 0. Chuẩn bị (làm trước khi bấm quay, không đưa vào video)

### 0.1 Backend và mạng
- [ ] Backend chạy (`http://localhost:5000/swagger` mở được), điện thoại và laptop **cùng Wi-Fi**, Windows Firewall mở cổng 5000.
- [ ] `http://<IP-laptop>:5000/api/health` mở được từ trình duyệt điện thoại thật (IP laptop: `ipconfig` → IPv4 của Wi-Fi).
- [ ] Điện thoại làm **SMS gateway** bật app SMS Gateway, SIM còn tiền (cho phần OTP SMS thật).

### 0.2 Chạy app trên 2 thiết bị (Android Studio, mở thư mục `mobile\nguyen_thanh_loi_2380601289`)

| Run Configuration | Dart entrypoint | Additional run args | Thiết bị |
|---|---|---|---|
| `emulator-admin` | `...\mobile\nguyen_thanh_loi_2380601289\lib\main.dart` | *(để trống)* | Emulator |
| `dien-thoai-that` | `...\mobile\nguyen_thanh_loi_2380601289\lib\main.dart` | `--dart-define=BASE_URL=http://<IP-laptop>:5000` | Điện thoại thật |

### 0.3 Dữ liệu cần có sẵn

| Cần | Cách có |
|---|---|
| Tài khoản `admin` / `Admin@123` | Do `DbSeeder` tạo khi backend khởi động |
| `manager1`, `employee1` | Có khi `Seed:ManagerPassword`, `Seed:EmployeePassword` được điền trong `appsettings.Development.json` (mật khẩu bạn tự đặt, **không để lộ trong video**) |
| `employee1` đã có **SĐT/Email đã xác nhận** | Để đăng nhập OTP |
| 1 **Ca làm** bao trùm giờ quay | Có thể tạo ngay trong video ở Phần 4 (Ca làm). Cho vào sớm tối đa 2 giờ trước giờ bắt đầu; vào sau giờ bắt đầu là **Đi muộn** |

### 0.4 Camera cho emulator (cần cho phần khuôn mặt)
Mở Device Manager → sửa AVD → Show Advanced Settings → Camera: **Front = Webcam0** (hoặc Emulated). Chạy emulator thử mở camera trước.

### 0.5 Điểm chấm công và vị trí (quan trọng)
Điểm chấm công do Admin đặt bằng nút **Đặt vị trí hiện tại làm điểm chấm công** ở Màn QR. Trên emulator nút này mở một **bản đồ để chọn điểm**: hãy chọn đúng chỗ bạn đang ngồi quay (ví dụ nhà bạn). Điện thoại thật dùng GPS thật nên phải ở **trong bán kính 200 m** quanh điểm đó. Chọn sai chỗ → GPS mất 45 điểm.

### 0.6 Máy quay
- Quay màn hình laptop (OBS hoặc Xbox Game Bar, `Win + G`), thu tiếng.
- Quay điện thoại thật: bật screen record trên điện thoại, **hoặc** để một người cầm điện thoại thứ hai quay cảnh quét QR. Cách đơn giản nhất: quay màn hình laptop và đưa điện thoại thật vào khung hình.
- Che mật khẩu, key, chuỗi kết nối. Không mở file `appsettings.Development.json` trong video.

---

## Kịch bản quay

### Phần 1 — Đăng ký tài khoản (emulator) · ~1:30
**Làm:** màn đăng nhập → **Chưa có tài khoản? Đăng ký** → điền Họ tên, Username, Email và/hoặc Số điện thoại, Mật khẩu, Nhập lại mật khẩu → **Đăng ký** → nhập OTP.

**Thấy được:**
- Để trống cả Email lẫn SĐT: nút xác nhận không bật, có báo lỗi.
- Điền SĐT hợp lệ: mã OTP **gửi thật** đến điện thoại (đưa tin nhắn vào khung hình), nhập đúng thì xác nhận xong.

**Nói:** "Đăng ký cần ít nhất Email hoặc SĐT. OTP hết hạn sau 5 phút, sai tối đa 5 lần, gửi lại cách nhau 60 giây, mỗi mã dùng một lần."

> Tài khoản vừa đăng ký là **Nhân viên**. Giữ lại để dùng ở Phần 7.2 (chấm công bị chờ duyệt).

### Phần 2 — Đăng nhập có OTP (emulator) · ~1:00
**Làm:** đăng xuất → đăng nhập `employee1` (username + mật khẩu) → nhập OTP SMS.

**Thấy được:** nhập sai OTP một lần có báo lỗi, nhập đúng thì vào màn **Nhân viên**.

### Phần 3 — Hồ sơ và khuôn mặt (emulator) · ~2:00
**Làm:**
1. Bấm biểu tượng **Hồ sơ** (góc trên). Chỉ nhãn **Đã xác nhận / Chưa xác nhận / Chưa có** của Email và Số điện thoại.
2. Ở kênh còn thiếu bấm thêm/xác nhận → **Gửi mã OTP** → nhập mã.
3. Ở mục **Khuôn mặt** bấm **Đăng ký khuôn mặt** → làm theo hướng dẫn, **chớp mắt** khi được yêu cầu → thấy **Đã đăng ký khuôn mặt**.
4. Đăng xuất → ở màn đăng nhập bấm **Đăng nhập bằng khuôn mặt** → nhập username → **chớp mắt để đăng nhập** → vào app.

**Nói:** "App phát hiện khuôn mặt bằng Google ML Kit và bắt buộc chớp mắt (mở – nhắm – mở) trong 5 giây để chống ảnh tĩnh. Khuôn mặt dùng để đăng nhập nhanh, không dùng trong luồng chấm công."

**Nói thẳng hạn chế:** vector khuôn mặt hiện là bản dự phòng dựng từ đường viền ML Kit, độ chính xác thấp hơn mô hình nhận dạng chuyên dụng.

### Phần 4 — Phân quyền và CRUD (emulator, vai Admin) · ~3:00
**Làm:** đăng xuất → đăng nhập `admin` / `Admin@123`. Dashboard **Quản trị** là lưới 9 ô.

1. Nhắc nhanh: Quản lý và Nhân viên vào dashboard khác (xem lại ở Phần 5 và 7).
2. **Nhân viên:** mở danh sách → thêm một nhân viên; ở form chỉ dropdown **Role** và **Chức vụ** (đổi Role thì danh sách Chức vụ thay đổi theo), **Phòng ban**, **Trạng thái** (Hoạt động/Khóa) → Lưu → sửa → xóa.
3. **Phòng ban**, **Chức vụ:** thêm một bản ghi rồi xóa. Thử xóa bản ghi **đang được dùng** → app báo bị chặn (không crash).
4. **Ca làm:** tạo ca có **Giờ bắt đầu / Giờ kết thúc** bao trùm giờ đang quay (cần cho Phần 7).
5. **Chấm công:** mở danh sách chấm công, lọc theo Nhân viên / Ca / Trạng thái, mở một bản ghi, sửa trạng thái.

**Nói:** "Admin không tự hạ quyền của mình và không xóa được admin cuối cùng. Quyền được đối chiếu với database ở mỗi request nên khóa tài khoản là có hiệu lực ngay."

### Phần 5 — Dashboard Quản lý, duyệt, vị trí (emulator, vai Quản lý) · ~1:30
**Làm:** đăng xuất → đăng nhập `manager1`.
- Dashboard **Quản lý**: số lượt chấm công hôm nay, danh sách **Chờ duyệt**, **Đi muộn hôm nay**, **Nhân viên phòng**.
- Bấm **Duyệt chi tiết** → màn **Duyệt chấm công** (hiện **Không có bản ghi chờ duyệt** nếu chưa có). Phần 7.2 sẽ có bản ghi để duyệt ở đây.
- Bấm **Vị trí nhân viên**: chưa ai chia sẻ thì hiện *Chưa có ai chia sẻ vị trí*. Phần 7.3 sẽ có điểm trên bản đồ.

### Phần 6 — Chuyển emulator sang vai Admin, hiển thị QR · ~1:00
**Làm:** đăng nhập `admin` (hoặc `manager1`) → ô **Màn QR** → **Màn QR chấm công**.

**Thấy được:**
- Mã QR đổi liên tục, dòng **Đổi mã sau N giây** đếm ngược 60 giây.
- Bấm **Đặt vị trí hiện tại làm điểm chấm công** → bản đồ chọn điểm (đúng chỗ bạn ngồi, xem 0.5) → xác nhận → thông báo `Đã thêm "Điểm chấm công N" ... bán kính 200 m`.

**Giữ màn hình này mở**, đặt emulator hoặc phóng to cửa sổ emulator cho dễ quét. Đây là "màn hình tại điểm chấm công" mà nhân viên sẽ quét.

### Phần 7 — Chấm công bằng điện thoại thật (điện thoại thật + emulator) · ~4:00

#### 7.1 Chấm công hợp lệ → 100 điểm
**Làm trên điện thoại thật:** đăng nhập `employee1` (có OTP SMS hoặc bằng khuôn mặt) → **Chấm công** → **Check-in** → app lấy vị trí → mở camera **Quét QR chấm công** → quét QR đang hiện trên emulator → màn kết quả.

**Thấy được:** màn kết quả có điểm tin cậy to ở giữa:
- GPS (cách ... m) **45/45**
- Mã QR **35/35**
- Không dùng GPS giả **20/20**
- Tổng **100/100**, trạng thái **Đúng giờ** (hoặc **Đi muộn** nếu quá giờ bắt đầu ca).

Quay cận cảnh màn kết quả trong 3 giây. Sau đó quay lại dashboard Nhân viên: thấy **Ca hôm nay**, **Điểm tin cậy lần gần nhất**, **Lịch sử chấm công tháng này**.

**Nói:** "Chấm công chỉ còn ba bước: lấy vị trí, quét QR, gửi. Điểm tin cậy gồm GPS 45, QR 35, không dùng GPS giả 20; từ 70 điểm trở lên là đạt."

#### 7.2 Chấm công bị nghi ngờ → chờ quản lý duyệt
Cần **một tài khoản nhân viên khác** (tài khoản đăng ký ở Phần 1) vì mỗi nhân viên chỉ chấm công một lần mỗi ca.

**Cách làm (QR quá hạn):**
1. Trên emulator đang hiện QR: chụp màn hình emulator (nút camera ở thanh công cụ emulator).
2. **Đợi hơn 60 giây** cho mã trong ảnh hết hạn.
3. Mở ảnh đó trên laptop. Trên điện thoại thật đăng nhập tài khoản mới → **Check-in** → quét QR **trong ảnh cũ**.

**Thấy được:** kết quả GPS 45, QR **0/35**, không GPS giả 20, tổng **65/100**, trạng thái **Chờ quản lý duyệt**.

Kết quả tương tự: đứng ngoài bán kính 200 m (55 điểm), hoặc bật GPS giả (35 điểm). QR hết hạn là cách làm dễ nhất.

**Làm tiếp trên emulator:** đăng nhập `manager1` → **Duyệt chấm công** → bản ghi vừa rồi hiện ra cùng **Điểm tin cậy 65** (có dấu hiệu "GPS giả / emulator" nếu có) → bấm **Duyệt** (hoặc **Từ chối** ở một bản ghi khác) → trạng thái đổi ngay.

**Nói:** "Quản lý chỉ duyệt được nhân viên phòng mình và không tự duyệt chấm công của chính mình."

#### 7.3 Chia sẻ vị trí, Check-out
- Điện thoại thật, màn **Chấm công** → **Chia sẻ vị trí hiện tại** → báo *Đã chia sẻ vị trí hiện tại*.
- Trên emulator (`manager1`) → **Vị trí nhân viên** → thấy nhân viên đó trên bản đồ/danh sách.
- Điện thoại thật → **Check-out** → báo *Đã check-out*.

### Phần 8 — Quay lại emulator, tổng kết · ~0:30
**Làm:** mở Admin → **Chấm công**: bản ghi vừa tạo xuất hiện với điểm tin cậy và trạng thái. Kết thúc video.

---

## Phần chưa có trong giao diện điện thoại (nói trước, đừng giấu)

| Nội dung | Tình trạng | Cách cho thầy thấy |
|---|---|---|
| **Báo cáo chấm công theo tháng** | Ô **Báo cáo** ở dashboard Admin hiện thông báo "làm ở Bước 14", chưa có màn hình | Mở Swagger → `GET /api/reports/monthly?month=2026-10` (đăng nhập bằng Admin, bấm Authorize) → trả số ngày công, số lần đi muộn/chờ duyệt/từ chối |
| **Đơn nghỉ phép** (duyệt/từ chối) | Có API, chưa có màn hình trên điện thoại | Swagger nhóm LeaveRequests |
| **Địa điểm** (nhiều điểm chấm công) | Tạo bằng nút ở Màn QR; chưa có màn sửa/xóa | Swagger nhóm Locations |
| **Đăng nhập với Google** | Chỉ chạy trên máy có SHA-1 đã đăng ký và `Google:WebClientId` | Nói rõ không demo được trên máy này |

Các kịch bản cần phần cứng thật (SMS thật, camera, GPS ngoài vùng) chưa được kiểm thử đầy đủ trên máy thật tại thời điểm nộp, đúng như mục Hạn chế trong README.

## Kịch bản rút gọn (nếu thầy giới hạn thời gian ~7 phút)
Phần 2 (đăng nhập OTP) → Phần 3 bước 3–4 (đăng ký và đăng nhập khuôn mặt) → Phần 4 (chỉ Nhân viên + Ca làm) → Phần 6 + 7.1 (QR và 100 điểm) → Phần 7.2 (65 điểm, quản lý duyệt).

## Kiểm nhanh trước khi nộp
- [ ] Có cảnh OTP SMS đến điện thoại thật.
- [ ] Màn kết quả chấm công hiện đủ ba dòng 45 / 35 / 20 và tổng 100.
- [ ] Có một bản ghi **Chờ quản lý duyệt** và thao tác Duyệt hoặc Từ chối của quản lý.
- [ ] Đăng ký và đăng nhập bằng khuôn mặt qua bước chớp mắt.
- [ ] Không có mật khẩu, key, chuỗi kết nối nào xuất hiện trong video.
- [ ] Đã nói rõ các phần nêu ở bảng "chưa có trong giao diện điện thoại".
