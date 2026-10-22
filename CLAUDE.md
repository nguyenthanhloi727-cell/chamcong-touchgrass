# CLAUDE.md — App chấm công (nhóm Touch grass)

Đọc `PLAN.md` (yêu cầu) và `PROGRESS.md` (các bước làm theo thứ tự) trước khi làm bất cứ việc gì.

## Cách chọn bước tiếp theo
- Làm theo prompt mẫu trong `PROGRESS.md`, **trừ Bước 0**: các mục còn lại của Bước 0 do người dùng tự xử lý, bỏ qua khi tìm bước tiếp theo.
- Bước tiếp theo = bước có số nhỏ nhất (từ Bước 1 trở đi) còn mục `[ ]` chưa tick.
- Xong mỗi bước: tick `[x]`, đổi trạng thái ở "Bảng tổng tiến độ", thêm 1 dòng "Nhật ký", commit đúng message ghi trong bước, push `main`.

## Tên và đường dẫn (bản gốc mang tên Lợi)
- Backend: `backend/NguyenThanhLoi_2380601289.ChamCong.Api/`, namespace `NguyenThanhLoi_2380601289.ChamCong.Api`
- Solution: `backend/ChamCong.slnx` (.NET 10 tạo `.slnx`, **không phải** `.sln`; `tool/rename.dart` phải sửa file này)
- Flutter: `mobile/nguyen_thanh_loi_2380601289/`, package `nguyen_thanh_loi_2380601289`, applicationId `com.touchgrass.nguyen_thanh_loi_2380601289`
- Dart import dùng **đường dẫn tương đối**, không dùng `package:<tên>/` (để rename không hỏng)

## Công nghệ đã chốt
- .NET 10 (SDK khóa trong `backend/global.json`), EF Core 10 + SQL Server, Swashbuckle 10.x
- `Microsoft.OpenApi` giữ **2.x** (Swashbuckle 10.3.1 dùng 2.x; đừng nâng lên 3.x)
- Enum lưu dạng chuỗi; mật khẩu BCrypt; seed user bằng `DbSeeder` lúc khởi động, không hash trong `HasData`
- OTP SMS gửi thật qua SMS Gateway for Android; `FakeSmsSender` chỉ dùng trong unit test

## Bí mật — repo PUBLIC
- Không commit: `appsettings.Development.json`, `appsettings.Local.json`, `.env`, `CLAUDE.local.md`
- Chỉ commit `appsettings.example.json` với giá trị rỗng; thêm key mới thì thêm vào cả file này
- Không hỏi giá trị bí mật trong chat (mật khẩu SQL, App Password, IP/user/pass gateway, JWT key, Google Client ID): tạo key rỗng trong `appsettings.Development.json` rồi bảo người dùng tự điền

## Commit
- Push thẳng `main`, mỗi bước 1 commit, message không dấu như trong `PROGRESS.md`
- Không sửa git config global; tác giả commit đã đặt trong config riêng của repo

## Dừng lại và hỏi khi
- Kiểm tra cần thao tác của người dùng (nhận SMS/mail, quét QR, chụp mặt, bấm trên điện thoại)
- Cần bí mật
- Kẹt 1 lỗi quá 20 phút → báo lỗi + phương án cắt
- 20:30 chưa tới Bước 18 → chuyển sang Bước 18; 21:30 → Bước 19
