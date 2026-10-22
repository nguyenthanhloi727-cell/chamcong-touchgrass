import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:nguyen_thanh_loi_2380601289/core/api/api_client.dart';
import 'package:nguyen_thanh_loi_2380601289/core/api/token_storage.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/auth_service.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/register_page.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/session_store.dart';

void main() {
  testWidgets('nút xác nhận chỉ bật khi kênh tương ứng hợp lệ; trống cả 2 báo lỗi',
      (tester) async {
    tester.view.physicalSize = const Size(800, 2400);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    final api = ApiClient(TokenStorage());
    final auth = AuthService(api, SessionStore(api));
    await tester.pumpWidget(MaterialApp(home: RegisterPage(auth: auth)));

    FilledButton btn(String text) => tester.widget<FilledButton>(
        find.ancestor(of: find.text(text), matching: find.byWidgetPredicate((w) => w is FilledButton)));

    expect(btn('Xác nhận qua Email').onPressed, isNull);
    expect(btn('Xác nhận qua SMS').onPressed, isNull);

    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), 'a@b.vn');
    await tester.pump();
    expect(btn('Xác nhận qua Email').onPressed, isNotNull);
    expect(btn('Xác nhận qua SMS').onPressed, isNull);

    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), '');
    await tester.enterText(
        find.widgetWithText(TextFormField, 'Số điện thoại'), '0912345678');
    await tester.pump();
    expect(btn('Xác nhận qua Email').onPressed, isNull);
    expect(btn('Xác nhận qua SMS').onPressed, isNotNull);

    await tester.enterText(
        find.widgetWithText(TextFormField, 'Số điện thoại'), '');
    await tester.pump();
    expect(find.text('Nhập ít nhất Email hoặc Số điện thoại'), findsWidgets);
  });
}
