import 'package:flutter/foundation.dart';

import '../../core/api/api_client.dart';
import 'auth_models.dart';

/// Người dùng đang đăng nhập; router lắng nghe để chuyển màn.
class SessionStore extends ChangeNotifier {
  final ApiClient api;
  UserInfo? _user;
  bool _restoring = true;

  SessionStore(this.api) {
    api.onSessionExpired = () => setUser(null);
  }

  UserInfo? get user => _user;
  bool get restoring => _restoring;
  bool get isLoggedIn => _user != null;

  void setUser(UserInfo? user) {
    _user = user;
    notifyListeners();
  }

  /// Mở app: có token thì hỏi /api/me để khôi phục phiên.
  Future<void> restore() async {
    try {
      if (await api.tokens.accessToken != null) {
        final res = await api.dio.get('/api/me');
        _user = UserInfo.fromJson(res.data as Map<String, dynamic>);
      }
    } catch (_) {
      _user = null;
    } finally {
      _restoring = false;
      notifyListeners();
    }
  }

  Future<void> logout() async {
    await api.tokens.clear();
    setUser(null);
  }
}
