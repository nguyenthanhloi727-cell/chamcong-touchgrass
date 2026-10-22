import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:nguyen_thanh_loi_2380601289/core/api/api_client.dart';
import 'package:nguyen_thanh_loi_2380601289/core/api/token_storage.dart';
import 'package:nguyen_thanh_loi_2380601289/core/router/app_router.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/auth_models.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/auth_service.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/session_store.dart';

UserInfo _user(String role, {String? position}) => UserInfo(
    id: 1, username: 'u', fullName: 'Ten $role', role: role, position: position);

class _NoToken extends TokenStorage {
  @override
  Future<String?> get accessToken async => null;
}

void main() {
  test('homeForRole', () {
    expect(homeForRole('Admin'), '/admin');
    expect(homeForRole('Manager'), '/manager');
    expect(homeForRole('Employee'), '/employee');
  });

  for (final (role, home) in [
    ('Admin', '/admin'),
    ('Manager', '/manager'),
    ('Employee', '/employee'),
  ]) {
    testWidgets('$role vào đúng màn và không sang màn role khác',
        (tester) async {
      final api = ApiClient(_NoToken());
      final session = SessionStore(api);
      await session.restore(); // hết trạng thái "đang khôi phục" (không có token)
      session.setUser(_user(role, position: 'Bảo vệ'));
      final router = buildRouter(session, AuthService(api, session));
      await tester.pumpWidget(MaterialApp.router(routerConfig: router));
      await tester.pump();
      expect(router.state.matchedLocation, home);
      expect(find.byKey(const Key('role-position')), findsOneWidget);

      for (final other in ['/admin', '/manager', '/employee']) {
        router.go(other);
        await tester.pump();
        expect(router.state.matchedLocation, home);
      }

      session.setUser(null);
      await tester.pump();
      expect(router.state.matchedLocation, '/login');
      await tester.pumpWidget(const SizedBox());
      await tester.pump(const Duration(seconds: 31));
    });
  }
}
