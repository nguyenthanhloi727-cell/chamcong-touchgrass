import 'package:google_sign_in/google_sign_in.dart';

import '../../core/api/api_client.dart';
import '../../core/config.dart';
import 'auth_models.dart';
import 'session_store.dart';

/// Gọi các API /api/auth/* và lưu token khi đăng nhập xong.
class AuthService {
  final ApiClient api;
  final SessionStore session;
  bool _googleReady = false;

  AuthService(this.api, this.session);

  Future<OtpSession> register({
    required String username,
    required String password,
    required String fullName,
    String? email,
    String? phone,
    required String verifyChannel, // "email" | "sms"
  }) async {
    final res = await api.dio.post('/api/auth/register', data: {
      'username': username,
      'password': password,
      'fullName': fullName,
      'email': email,
      'phone': phone,
      'verifyChannel': verifyChannel,
    });
    return OtpSession.fromJson(res.data as Map<String, dynamic>);
  }

  /// Trả null khi đăng nhập xong luôn (Admin bỏ qua OTP); ngược lại trả phiên OTP.
  Future<OtpSession?> login(String username, String password) async {
    final res = await api.dio.post('/api/auth/login',
        data: {'username': username, 'password': password});
    final data = res.data as Map<String, dynamic>;
    final auth = data['auth'] as Map<String, dynamic>?;
    if (auth == null) return OtpSession.fromJson(data);
    await api.tokens
        .save(auth['accessToken'] as String, auth['refreshToken'] as String);
    session.setUser(UserInfo.fromJson(auth['user'] as Map<String, dynamic>));
    return null;
  }

  /// Gửi lại OTP; backend trả phiên mới (cùng kênh).
  Future<OtpSession> resendOtp(int otpSessionId) async {
    final res = await api.dio
        .post('/api/auth/resend-otp', data: {'otpSessionId': otpSessionId});
    return OtpSession.fromJson(res.data as Map<String, dynamic>);
  }

  /// Trả purpose ("Register" | "Login" | "AddContact"). Có token thì lưu và vào app.
  Future<String> verifyOtp(int otpSessionId, String code) async {
    final res = await api.dio.post('/api/auth/verify-otp',
        data: {'otpSessionId': otpSessionId, 'code': code});
    final data = res.data as Map<String, dynamic>;
    final auth = data['auth'] as Map<String, dynamic>?;
    final user = UserInfo.fromJson(data['user'] as Map<String, dynamic>);
    if (auth != null) {
      await api.tokens
          .save(auth['accessToken'] as String, auth['refreshToken'] as String);
      session.setUser(user);
    } else {
      // AddContact: làm mới thông tin người dùng hiện tại
      session.setUser(user);
    }
    return data['purpose'] as String;
  }

  /// Đăng nhập bằng khuôn mặt; sai mặt thì backend trả 401 và ném lỗi.
  Future<void> loginWithFace(String username, List<double> embedding) async {
    final res = await api.dio.post('/api/face/login',
        data: {'username': username, 'embedding': embedding});
    final data = res.data as Map<String, dynamic>;
    await api.tokens
        .save(data['accessToken'] as String, data['refreshToken'] as String);
    session.setUser(UserInfo.fromJson(data['user'] as Map<String, dynamic>));
  }

  /// Trả false nếu người dùng hủy chọn tài khoản Google.
  Future<bool> loginWithGoogle() async {
    if (googleWebClientId.isEmpty) {
      throw 'Chưa cấu hình GOOGLE_WEB_CLIENT_ID (--dart-define)';
    }
    final google = GoogleSignIn.instance;
    if (!_googleReady) {
      await google.initialize(serverClientId: googleWebClientId);
      _googleReady = true;
    }
    final GoogleSignInAccount account;
    try {
      account = await google.authenticate();
    } on GoogleSignInException catch (e) {
      if (e.code == GoogleSignInExceptionCode.canceled) return false;
      throw 'Đăng nhập Google thất bại: ${e.description ?? e.code.name}';
    }
    final idToken = account.authentication.idToken;
    if (idToken == null) throw 'Google không trả idToken';
    final res = await api.dio.post('/api/auth/google', data: {'idToken': idToken});
    final data = res.data as Map<String, dynamic>;
    await api.tokens
        .save(data['accessToken'] as String, data['refreshToken'] as String);
    session.setUser(UserInfo.fromJson(data['user'] as Map<String, dynamic>));
    return true;
  }
}
