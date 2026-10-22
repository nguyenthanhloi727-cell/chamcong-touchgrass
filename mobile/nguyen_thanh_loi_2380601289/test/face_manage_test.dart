import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:nguyen_thanh_loi_2380601289/core/api/api_client.dart';
import 'package:nguyen_thanh_loi_2380601289/core/api/token_storage.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/auth_models.dart';
import 'package:nguyen_thanh_loi_2380601289/features/auth/session_store.dart';
import 'package:nguyen_thanh_loi_2380601289/features/face/face_enroll_page.dart';

class _NoToken extends TokenStorage {
  @override
  Future<String?> get accessToken async => null;
}

Future<SessionStore> _session({required bool hasFace}) async {
  final s = SessionStore(ApiClient(_NoToken()));
  await s.restore();
  s.setUser(UserInfo(
      id: 1,
      username: 'u',
      fullName: 'Nhân viên',
      role: 'Employee',
      hasFace: hasFace));
  return s;
}

void main() {
  testWidgets('chưa đăng ký: chỉ có nút Đăng ký, không có Thử nhận diện / Xóa',
      (tester) async {
    final s = await _session(hasFace: false);
    await tester.pumpWidget(MaterialApp(home: FaceEnrollPage(session: s)));
    await tester.pumpAndSettle();

    expect(find.text('Chưa đăng ký khuôn mặt'), findsOneWidget);
    expect(find.text('Đăng ký khuôn mặt'), findsOneWidget);
    expect(find.text('Thử nhận diện'), findsNothing);
    expect(find.text('Xóa khuôn mặt'), findsNothing);
  });

  testWidgets('đã đăng ký: có Đăng ký lại, Thử nhận diện, Xóa; Xóa hỏi xác nhận',
      (tester) async {
    final s = await _session(hasFace: true);
    await tester.pumpWidget(MaterialApp(home: FaceEnrollPage(session: s)));
    await tester.pumpAndSettle();

    expect(find.text('Đã đăng ký khuôn mặt'), findsOneWidget);
    expect(find.text('Đăng ký lại'), findsOneWidget);
    expect(find.text('Thử nhận diện'), findsOneWidget);
    expect(find.text('Xóa khuôn mặt'), findsOneWidget);

    await tester.tap(find.text('Xóa khuôn mặt'));
    await tester.pumpAndSettle();
    expect(find.text('Xác nhận xóa'), findsOneWidget);
    await tester.tap(find.text('Hủy'));
    await tester.pumpAndSettle();
    expect(find.text('Xác nhận xóa'), findsNothing);
    expect(s.user!.hasFace, isTrue); // hủy thì không xóa
  });

  testWidgets('màn tự cập nhật khi hồ sơ đổi trạng thái khuôn mặt',
      (tester) async {
    final s = await _session(hasFace: false);
    await tester.pumpWidget(MaterialApp(home: FaceEnrollPage(session: s)));
    await tester.pumpAndSettle();
    expect(find.text('Xóa khuôn mặt'), findsNothing);

    s.setUser(UserInfo(
        id: 1, username: 'u', fullName: 'Nhân viên', role: 'Employee', hasFace: true));
    await tester.pumpAndSettle();
    expect(find.text('Xóa khuôn mặt'), findsOneWidget);
  });
}
