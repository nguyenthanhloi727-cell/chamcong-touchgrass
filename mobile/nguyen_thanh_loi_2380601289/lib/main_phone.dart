import 'core/config.dart' as config;
import 'main.dart' as app;

/// Chạy trên điện thoại thật (đóng vai Nhân viên quét QR).
/// Đổi IP bên dưới thành IP LAN của laptop (ipconfig → IPv4 của Wi-Fi); cùng Wi-Fi, mở cổng 5000.
void main() {
  config.baseUrl = 'http://192.168.1.5:5000';
  app.main();
}
