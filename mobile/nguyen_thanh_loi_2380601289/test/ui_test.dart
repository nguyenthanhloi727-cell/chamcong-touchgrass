import 'package:flutter_test/flutter_test.dart';

import 'package:nguyen_thanh_loi_2380601289/core/widgets/ui.dart';

void main() {
  test('initials lấy chữ cái đầu của họ và tên, bỏ phần trong ngoặc', () {
    expect(initials('Quản trị viên (admin)'), 'QV');
    expect(initials('Nhân viên mẫu (employee1)'), 'NM');
    expect(initials('Thanh Loi'), 'TL');
    expect(initials('admin'), 'A');
    expect(initials('  '), '?');
    expect(initials('(admin)'), '?');
  });
}
