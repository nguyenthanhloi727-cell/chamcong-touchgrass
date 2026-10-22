# Kịch bản demo & quay video — App chấm công (nhóm Touch grass)

Mục tiêu: quay một video ~10 phút cho thấy app **chạy thật, đủ 7 mục của đề**. Không đọc từng bước, mỗi cảnh chỉ cần cho người xem thấy **việc gì xảy ra và kết quả là gì**.

## Chuẩn bị trước khi bấm quay

| Việc | Ghi chú |
|---|---|
| Backend đang chạy, `http://<IP-laptop>:5000/api/health` trả OK | Điện thoại và laptop cùng Wi-Fi |
| 2 điện thoại (hoặc 1 điện thoại + 1 trình duyệt) | 1 máy Nhân viên, 1 máy/trình duyệt Admin để hiện mã QR |
| Điện thoại gateway SMS bật, SIM còn tiền | Cho cảnh OTP SMS thật |
| 3 tài khoản seed: `admin`, `manager1`, `employee1` | Mật khẩu `admin` trong README; 2 tài khoản còn lại do bạn đặt ở `Seed:*Password` |
| Có sẵn 1 Địa điểm đúng vị trí đang đứng quay | Cảnh GPS "trong vùng" |
| Bật ghi màn hình điện thoại + ghi tiếng | Quay ngang để thấy cả thanh trạng thái |

Mẹo: chạy thử cả kịch bản một lượt trước. Lỗi nào cần điện thoại thật (SMS, chụp mặt, QR, GPS) thì phải thấy ở lượt chạy thử, không để lộ trong lúc quay chính thức.

---

## Cảnh 1 — Đăng nhập có OTP (mục 2) · ~1:30

**Làm:** đăng nhập `employee1` bằng username + mật khẩu → điện thoại gateway gửi SMS → nhập OTP.

**Cho người xem thấy:**
- Tin nhắn OTP đến **thật** trên điện thoại.
- Nhập sai OTP một lần: báo lỗi rõ ràng, nhập đúng thì vào được màn Nhân viên.

**Nói:** "OTP hết hạn sau 5 phút, sai tối đa 5 lần, gửi lại cách nhau 60 giây, mỗi mã dùng một lần."

## Cảnh 2 — Đăng ký tài khoản mới (mục 1) · ~1:30

**Làm:** đăng ký một tài khoản với **Email hoặc SĐT** (ít nhất một) → xác nhận bằng OTP qua kênh vừa chọn.

**Cho người xem thấy:**
- Bỏ trống cả Email lẫn SĐT: nút xác nhận không bật, có báo lỗi.
- Có SĐT/Email hợp lệ: OTP về đúng kênh, xác nhận xong tài khoản dùng được.

## Cảnh 3 — Phân quyền theo vai trò (mục 3) · ~1:00

**Làm:** lần lượt đăng nhập `admin`, `manager1`, `employee1`.

**Cho người xem thấy:** mỗi vai trò vào **một dashboard khác nhau**, Nhân viên không thấy chức năng quản trị. Ở Admin mở danh sách nhân viên, thấy dropdown Vai trò và dropdown Chức vụ lọc theo vai trò.

**Nói:** "Admin không tự hạ quyền và không xóa được admin cuối cùng; quyền được đối chiếu với database ở mỗi request."

## Cảnh 4 — CRUD (mục 4) · ~1:30

**Làm:** với Admin, thêm / sửa / xóa trên **Phòng ban**, **Chức vụ**, **Ca làm**, **Nhân viên**; xem danh sách **Chấm công**.

**Cho người xem thấy:** một thao tác trọn vòng (thêm → sửa → xóa) ở ít nhất 2 mục, và **xóa bản ghi còn đang được dùng thì bị chặn** với thông báo rõ (không crash).

## Cảnh 5 — Chấm công bằng GPS + QR (mục 5b, 5e) · ~2:00

Đây là cảnh chính. Dùng hai máy: Admin hiển thị QR, Nhân viên quét.

**Làm:**
1. Máy Admin: mở **Màn QR chấm công** (QR động); chỉ cho thấy mã đổi sau mỗi 30 giây.
2. Máy Nhân viên: bấm **Check-in** → app lấy vị trí → quét QR.
3. Xem **màn kết quả**.

**Cho người xem thấy màn kết quả có điểm tin cậy:** GPS `45/45`, Mã QR `35/35`, Không dùng GPS giả `20/20`, tổng `100/100`, trạng thái **Đúng giờ** (hoặc **Đi muộn** nếu quay sau giờ bắt đầu ca).

**Nói:** "Quy trình chấm công chỉ còn vị trí, quét QR và gửi. Khuôn mặt không nằm trong luồng này nữa mà dùng cho đăng nhập nhanh."

Chạy thêm 1 lần nữa (tài khoản/ca khác) để cho thấy **trường hợp bị nghi ngờ** → xem Cảnh 6.

## Cảnh 6 — Điểm tin cậy và duyệt (mục 6) · ~1:30

Cho thấy điểm tin cậy **thực sự ảnh hưởng** kết quả, không chỉ hiển thị.

**Làm:** chấm công với một điều kiện sai, rồi Quản lý xử lý.

| Tình huống | Điểm | Kết quả |
|---|---|---|
| Đủ GPS + QR + không GPS giả | 100 | Đúng giờ / Đi muộn |
| QR quá hạn (quét mã cũ) | 65 | **Chờ quản lý duyệt** |
| Đứng ngoài vùng GPS | 55 | **Chờ quản lý duyệt** |
| Bật GPS giả (mock location) | 35 | **Chờ quản lý duyệt** |

Chỉ cần quay **một** trường hợp (khuyên dùng QR quá hạn: dễ làm nhất). Sau đó đăng nhập `manager1`, mở **Duyệt chấm công**, bấm **Duyệt** và bấm **Từ chối** ở hai bản ghi khác nhau; trạng thái đổi ngay.

**Nói:** "Ngưỡng đạt là 70 điểm; GPS 45, QR 35, không GPS giả 20. Dưới ngưỡng thì phải có quản lý duyệt. Quản lý chỉ duyệt được nhân viên phòng mình và không tự duyệt chấm công của chính mình."

## Cảnh 7 — Khuôn mặt (mục 5a) · ~1:30

**Làm:**
1. Vào hồ sơ tài khoản → **Đăng ký khuôn mặt** (chớp mắt khi được yêu cầu).
2. Đăng xuất → ở màn đăng nhập bấm **Đăng nhập bằng khuôn mặt**.

**Cho người xem thấy:** app bắt **chớp mắt** (giơ ảnh tĩnh thì không qua) rồi đăng nhập thành công.

**Nói thẳng hạn chế:** "Vector khuôn mặt hiện là bản dự phòng dựng từ đường viền của ML Kit, độ chính xác thấp hơn mô hình chuyên dụng." (đúng như mục Hạn chế trong README)

## Cảnh 8 — Hồ sơ, vị trí nhóm và báo cáo (mục 7) · ~1:00

**Làm:**
- Hồ sơ: chỉ ra nhãn **Đã xác nhận / Chưa xác nhận / Chưa có** của Email và SĐT; bổ sung một kênh còn thiếu bằng OTP.
- Nhân viên bấm **Chia sẻ vị trí hiện tại**; Quản lý mở **Vị trí nhân viên** thấy người đó.
- Quản lý mở **báo cáo tháng**: số ngày công, số lần đi muộn, số lần chờ duyệt, số lần bị từ chối.

---

## Kịch bản gọn (nếu chỉ có 5 phút)

Cảnh 1 (đăng nhập OTP) → Cảnh 3 (3 vai trò, lướt nhanh) → Cảnh 5 (check-in 100/100) → Cảnh 6 (một ca bị chờ duyệt rồi duyệt) → Cảnh 7 (đăng nhập bằng mặt).

## Kiểm nhanh trước khi nộp

- [ ] Thấy OTP SMS thật trên điện thoại ở cảnh 1.
- [ ] Màn kết quả chấm công hiện đủ 3 dòng 45 / 35 / 20 và tổng 100.
- [ ] Có ít nhất một bản ghi **Chờ quản lý duyệt** được duyệt và một bản bị từ chối.
- [ ] Đăng nhập bằng mặt qua được bước chớp mắt.
- [ ] Không để lộ mật khẩu, key hay chuỗi kết nối trong video (che `appsettings.Development.json`).

## Những phần chưa kiểm thử trên máy thật

Theo README mục Hạn chế: Google Sign-In (cần SHA-1 đã đăng ký) và một số kịch bản cần phần cứng thật (GPS ngoài vùng, chụp mặt) chưa được thử đầy đủ. Nếu video không quay được các cảnh này thì nói rõ ở lời dẫn thay vì bỏ qua.
