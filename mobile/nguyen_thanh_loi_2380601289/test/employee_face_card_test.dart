import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:nguyen_thanh_loi_2380601289/core/api/api_client.dart';
import 'package:nguyen_thanh_loi_2380601289/core/api/token_storage.dart';
import 'package:nguyen_thanh_loi_2380601289/core/theme/app_theme.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/auth_models.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/session_store.dart';
import 'package:nguyen_thanh_loi_2380601289/features/dashboard/employee_dashboard.dart';

class _NoToken extends TokenStorage {
  @override
  Future<String?> get accessToken async => null;
}

/// Backend giả: không ca làm, không lịch sử chấm công.
class _EmptyBackend implements HttpClientAdapter {
  @override
  void close({bool force = false}) {}

  @override
  Future<ResponseBody> fetch(RequestOptions o, Stream<Uint8List>? s, Future<void>? c) async =>
      ResponseBody.fromString(jsonEncode([]), 200,
          headers: {Headers.contentTypeHeader: ['application/json']});
}

UserInfo _user({required bool hasFace}) => UserInfo(
    id: 1,
    username: 'employee1',
    fullName: 'Nhân viên mẫu',
    role: 'Employee',
    position: 'Kế toán',
    hasFace: hasFace);

Future<SessionStore> _pump(WidgetTester tester, {required bool hasFace}) async {
  tester.view.physicalSize = const Size(1080, 2340);
  tester.view.devicePixelRatio = 3;
  addTearDown(tester.view.reset);

  final api = ApiClient(_NoToken());
  api.dio.httpClientAdapter = _EmptyBackend();
  final session = SessionStore(api);
  await session.restore();
  session.setUser(_user(hasFace: hasFace));
  await tester.pumpWidget(MaterialApp(
    theme: buildAppTheme(),
    home: EmployeeDashboard(session: session),
  ));
  await tester.pumpAndSettle();
  return session;
}

void main() {
  testWidgets('chưa đăng ký khuôn mặt: banner "Đăng ký ngay" ở đầu dashboard',
      (tester) async {
    await _pump(tester, hasFace: false);

    expect(tester.takeException(), isNull);
    expect(find.byKey(const Key('face-banner')), findsOneWidget);
    expect(find.text('Đăng ký khuôn mặt'), findsOneWidget);
    expect(find.text('Đăng ký ngay'), findsOneWidget);
    expect(find.text('Chấm công'), findsWidgets); // nút chấm công vẫn còn
    // Banner nằm trên nút Chấm công
    final banner = tester.getTopLeft(find.byKey(const Key('face-banner'))).dy;
    final checkin = tester.getTopLeft(find.text('Chấm công').first).dy;
    expect(banner, lessThan(checkin));
  });

  testWidgets('đã đăng ký: banner biến mất, hiện ô "Khuôn mặt · Đã đăng ký"',
      (tester) async {
    await _pump(tester, hasFace: true);

    expect(tester.takeException(), isNull);
    expect(find.byKey(const Key('face-banner')), findsNothing);
    expect(find.text('Khuôn mặt'), findsOneWidget);
    expect(find.text('Đã đăng ký · bấm để quản lý'), findsOneWidget);
  });

  testWidgets('đăng ký xong (hồ sơ đổi) thì banner tự biến mất', (tester) async {
    final session = await _pump(tester, hasFace: false);
    expect(find.byKey(const Key('face-banner')), findsOneWidget);

    session.setUser(_user(hasFace: true));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('face-banner')), findsNothing);
    expect(find.text('Đã đăng ký · bấm để quản lý'), findsOneWidget);
  });
}
