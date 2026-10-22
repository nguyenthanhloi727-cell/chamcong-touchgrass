# App chấm công nhân viên — nhóm Touch grass

| Thành viên | MSSV |
|---|---|
| Nguyễn Thành Lợi | 2380601289 |
| Hồ Ngọc Phú | 2380601699 |
| Phạm Đinh Gia Bảo | 2380600166 |
| Phương Bảo Khôi | 2380601104 |

**Stack:** Flutter (Android) · ASP.NET Core Web API (.NET 10) · EF Core · SQL Server

## Cấu trúc

```
backend/NguyenThanhLoi_2380601289.ChamCong.Api/   Web API
mobile/nguyen_thanh_loi_2380601289/               Flutter app
```

_Hướng dẫn chạy, tài khoản demo: cập nhật ở bước cuối._

## Khuôn mặt (đăng ký, đăng nhập, chấm công)

- Phát hiện mặt + xác suất mở mắt: Google ML Kit Face Detection (`google_mlkit_face_detection`). Bắt buộc chớp mắt (mở → nhắm → mở) trong 5 giây để chống ảnh tĩnh.
- **Embedding hiện là bản dự phòng, không phải MobileFaceNet:** vector hình dạng từ các đường viền ML Kit (mặt, lông mày, mắt, môi, mũi), xoay theo góc nghiêng đầu, chuẩn hóa tâm/kích thước/độ dài 1. Độ chính xác thấp hơn MobileFaceNet; chưa có file model `.tflite` nào trong repo (nguồn và license chưa chốt).
- Backend so cosine similarity với ngưỡng `Face:Threshold` (`appsettings`); cần chỉnh theo thử nghiệm thật vì embedding dự phòng cho điểm tương đồng cao giữa các khuôn mặt.
