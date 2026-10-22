import 'package:flutter_test/flutter_test.dart';
import 'package:nguyen_thanh_loi_2380601289/features/attendance/geo_service.dart';

void main() {
  test('distanceMeters: 0 cho cùng điểm, ~111 m cho 0.001 độ vĩ', () {
    expect(distanceMeters(10, 106, 10, 106), 0);
    expect(distanceMeters(10, 106, 10.001, 106), closeTo(111.2, 1));
  });
}
