import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nguyen_thanh_loi_2380601289/core/api/api_client.dart';
import 'package:nguyen_thanh_loi_2380601289/core/api/token_storage.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/auth_models.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/auth_service.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/session_store.dart';
import 'package:nguyen_thanh_loi_2380601289/features/profile/profile_page.dart';

void main() {
  testWidgets('Hồ sơ hiện nhãn trạng thái Email / SĐT', (tester) async {
    final session = SessionStore(ApiClient(TokenStorage()));
    session.setUser(const UserInfo(
      id: 1,
      username: 'u',
      fullName: 'Nguyen Van A',
      email: 'a@x.com',
      emailVerified: true,
      role: 'Employee',
    ));
    final auth = AuthService(session.api, session);

    await tester.pumpWidget(MaterialApp(
        home: ProfilePage(session: session, auth: auth)));

    expect(find.textContaining('Đã xác nhận'), findsOneWidget);
    expect(find.text('Chưa có'), findsOneWidget); // SĐT
    expect(find.text('Thêm'), findsOneWidget);
    expect(find.text('Đăng ký khuôn mặt'), findsWidgets);

    // Email chưa xác nhận → nhãn đổi, có nút Xác nhận
    session.setUser(const UserInfo(
      id: 1,
      username: 'u',
      fullName: 'Nguyen Van A',
      email: 'a@x.com',
      phoneNumber: '+84901234567',
      role: 'Employee',
    ));
    await tester.pump();
    expect(find.textContaining('Chưa xác nhận'), findsNWidgets(2));
    expect(find.text('Xác nhận'), findsNWidgets(2));
  });
}
