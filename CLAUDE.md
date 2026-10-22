# CLAUDE.md — App chấm công (nhóm Touch grass)

Đọc `PLAN.md` (yêu cầu) và `PROGRESS.md` (các bước làm theo thứ tự) trước khi làm bất cứ việc gì.

## Quy trình: MỖI CONTEXT LÀM ĐÚNG 1 BƯỚC
Quy trình này **thay cho** câu "LÀM LUÔN Bước N+1" trong prompt mẫu của `PROGRESS.md`.

1. **Chọn bước:** người dùng ghi bước nào thì làm bước đó. Không ghi số → lấy bước có số nhỏ nhất (từ Bước 1 trở đi) còn mục `[ ]`. **Bỏ qua Bước 0** (người dùng tự xử lý).
2. **Làm:** báo "Đang làm Bước N — <tên>", làm hết các mục N.x chưa tick. Chỉ làm trong phạm vi bước đó, không lan sang bước khác.
3. **Tự test:** chạy đúng phần "Kiểm tra" của bước. Lỗi thì sửa đến khi qua. Phần kiểm tra cần thao tác của người dùng → dừng, nói rõ cần làm gì, chờ kết quả rồi mới tick.
4. **Test OK → tự ghi `PROGRESS.md`:** tick `[x]` các mục, đổi trạng thái ở "Bảng tổng tiến độ" sang ✅, thêm 1 dòng "Nhật ký" (giờ, bước, kết quả).
5. **Tự commit + push:** commit đúng message ghi trong bước, push `main`. Không cần hỏi.
6. **Báo và DỪNG:** viết "✅ Xong Bước N. Bước tiếp theo: **Bước M — <tên>**. Mở context mới và gõ: `Làm Bước M theo CLAUDE.md`." Rồi **dừng, không làm Bước M** trong context này.
7. Lặp lại ở context mới đến khi hết `PROGRESS.md` (Bước 19).

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
