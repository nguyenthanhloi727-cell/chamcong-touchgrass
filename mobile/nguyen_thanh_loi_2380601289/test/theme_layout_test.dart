import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:nguyen_thanh_loi_2380601289/core/api/api_client.dart';
import 'package:nguyen_thanh_loi_2380601289/core/api/token_storage.dart';
import 'package:nguyen_thanh_loi_2380601289/core/theme/app_theme.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/auth_models.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/auth_service.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/session_store.dart';
import 'package:nguyen_thanh_loi_2380601289/features/profile/profile_page.dart';

/// Chạy với theme THẬT của app, kích thước điện thoại: test cũ dùng MaterialApp trần nên
/// không bắt được lỗi layout do theme (nút rộng vô hạn nuốt cả ListTile / Row, màn trắng).
void main() {
  testWidgets('Hồ sơ dựng được với theme app (nút trong ListTile.trailing)',
      (tester) async {
    tester.view.physicalSize = const Size(1080, 2340);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final session = SessionStore(ApiClient(TokenStorage()));
    session.setUser(const UserInfo(
      id: 1,
      username: 'u',
      fullName: 'Nguyen Van A',
      email: 'a@x.com',
      role: 'Employee',
    ));
    final auth = AuthService(session.api, session);

    await tester.pumpWidget(MaterialApp(
      theme: buildAppTheme(),
      home: ProfilePage(session: session, auth: auth),
    ));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.text('Thêm'), findsOneWidget); // nút trong trailing hiện được
  });

  testWidgets('FilledButton trong Row phải ghi đè minimumSize (không thì vỡ layout)',
      (tester) async {
    await tester.pumpWidget(MaterialApp(
      theme: buildAppTheme(),
      home: Scaffold(
        body: Row(children: [
          const Expanded(child: Text('toạ độ')),
          FilledButton(
            style: FilledButton.styleFrom(minimumSize: const Size(0, 48)),
            onPressed: () {},
            child: const Text('Dùng vị trí này'),
          ),
        ]),
      ),
    ));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(find.text('Dùng vị trí này'), findsOneWidget);
  });
}
