import 'package:flutter_test/flutter_test.dart';

import 'package:nguyen_thanh_loi_2380601289/core/api/api_client.dart';

void main() {
  test('apiErrorMessage đọc trường error của backend', () {
    expect(apiErrorMessage('x'), 'x');
  });
}
