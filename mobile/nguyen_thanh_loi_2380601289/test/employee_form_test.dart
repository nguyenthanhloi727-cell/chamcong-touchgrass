import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:nguyen_thanh_loi_2380601289/core/api/api_client.dart';
import 'package:nguyen_thanh_loi_2380601289/core/api/token_storage.dart';
import 'package:nguyen_thanh_loi_2380601289/features/crud/employees_page.dart';

class _NoToken extends TokenStorage {
  @override
  Future<String?> get accessToken async => null;
}

/// Trả dữ liệu cố định cho /api/positions và /api/departments, ghi lại các request còn lại.
class _FakeAdapter implements HttpClientAdapter {
  final requests = <String>[];

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? body,
      Future<void>? cancel) async {
    requests.add('${options.method} ${options.path}');
    final data = switch (options.path) {
      '/api/positions' => [
          {'id': 1, 'name': 'Trưởng phòng Nhân sự', 'roleGroup': 'Manager'},
          {'id': 2, 'name': 'Quản lý Kho', 'roleGroup': 'Manager'},
          {'id': 3, 'name': 'Kế toán', 'roleGroup': 'Employee'},
          {'id': 4, 'name': 'Bảo vệ', 'roleGroup': 'Employee'},
        ],
      '/api/departments' => [
          {'id': 1, 'name': 'Phòng A', 'employeeCount': 0},
        ],
      _ => <Object>[],
    };
    return ResponseBody.fromString(jsonEncode(data), 200, headers: {
      Headers.contentTypeHeader: ['application/json'],
    });
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  testWidgets('Đổi Role (ô 1) → danh sách Chức vụ (ô 2) lọc đúng và reset',
      (tester) async {
    tester.view.physicalSize = const Size(800, 2400);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    final api = ApiClient(_NoToken());
    api.dio.httpClientAdapter = _FakeAdapter();
    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: Builder(
          builder: (context) => TextButton(
            onPressed: () => showDialog<bool>(
                context: context, builder: (_) => EmployeeForm(api: api)),
            child: const Text('mở'),
          ),
        ),
      ),
    ));
    await tester.tap(find.text('mở'));
    await tester.pumpAndSettle();

    Future<List<String>> openPositionOptions() async {
      await tester.tap(find.text('Chức vụ'));
      await tester.pumpAndSettle();
      final names = ['Trưởng phòng Nhân sự', 'Quản lý Kho', 'Kế toán', 'Bảo vệ']
          .where((n) => find.text(n).evaluate().isNotEmpty)
          .toList();
      return names;
    }

    // Mặc định Role = Nhân viên → chỉ chức vụ nhóm Employee.
    expect(await openPositionOptions(), ['Kế toán', 'Bảo vệ']);
    await tester.tap(find.text('Kế toán').last);
    await tester.pumpAndSettle();

    // Đổi Role sang Quản lý → ô 2 reset, chỉ còn chức vụ nhóm Manager.
    await tester.tap(find.byKey(const Key('role-dropdown')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Quản lý').last);
    await tester.pumpAndSettle();
    expect(find.text('Kế toán'), findsNothing);
    expect(await openPositionOptions(),
        ['Trưởng phòng Nhân sự', 'Quản lý Kho']);
  });
}
