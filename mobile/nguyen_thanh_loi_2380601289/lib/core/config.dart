/// Địa chỉ backend. Mặc định (emulator): http://10.0.2.2:5000 · máy thật: IP LAN của laptop.
/// Đổi bằng: flutter run --dart-define=BASE_URL=http://192.168.1.10:5000
/// hoặc chạy qua lib/main_emulator.dart / lib/main_phone.dart (gán lại biến này trước khi chạy app).
String baseUrl = const String.fromEnvironment(
  'BASE_URL',
  defaultValue: 'http://10.0.2.2:5000',
);

/// OAuth client loại Web trên Google Cloud Console (dùng làm serverClientId).
/// Truyền bằng: --dart-define=GOOGLE_WEB_CLIENT_ID=...  (không commit giá trị thật)
const String googleWebClientId = String.fromEnvironment('GOOGLE_WEB_CLIENT_ID');
