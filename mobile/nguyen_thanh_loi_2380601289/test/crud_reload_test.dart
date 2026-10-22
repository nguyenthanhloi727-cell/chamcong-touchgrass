import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:nguyen_thanh_loi_2380601289/features/crud/crud_common.dart';

void main() {
  testWidgets('Lưu xong danh sách tự tải lại ngay', (tester) async {
    final db = <Json>[
      {'id': 1, 'name': 'A'}
    ];
    await tester.pumpWidget(MaterialApp(
      home: CrudListPage(
        title: 'T',
        load: (q) async => List.of(db),
        titleOf: (e) => e['name'] as String,
        subtitleOf: (e) => '',
        form: (context, item) => showDialog<bool>(
          context: context,
          builder: (_) {
            final key = GlobalKey<FormState>();
            return FormDialog(
              title: 'F',
              formKey: key,
              onSubmit: () async => db.add({'id': 2, 'name': 'B'}),
              children: const [Text('x')],
            );
          },
        ),
        onDelete: (e) async {},
      ),
    ));
    await tester.pumpAndSettle();
    expect(find.text('B'), findsNothing);
    await tester.tap(find.byType(FloatingActionButton));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Lưu'));
    await tester.pumpAndSettle();
    // Avatar chữ cái đầu cũng hiện 'B', nên đếm số dòng danh sách (A và B) thay vì đếm chữ.
    expect(find.byType(ListTile), findsNWidgets(2));
    expect(find.text('B'), findsWidgets);
  });
}
