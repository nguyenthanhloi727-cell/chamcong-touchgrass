import 'core/config.dart' as config;
import 'main.dart' as app;

/// Chạy trên emulator (đóng vai Admin hiển thị QR). 10.0.2.2 = laptop nhìn từ emulator.
void main() {
  config.baseUrl = 'http://10.0.2.2:5000';
  app.main();
}
