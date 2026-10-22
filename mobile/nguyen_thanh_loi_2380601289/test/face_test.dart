import 'dart:math' as math;

import 'package:flutter_test/flutter_test.dart';
import 'package:nguyen_thanh_loi_2380601289/features/face/blink_detector.dart';
import 'package:nguyen_thanh_loi_2380601289/features/face/face_embedding.dart';

double cosine(List<double> a, List<double> b) {
  var d = 0.0;
  for (var i = 0; i < a.length; i++) {
    d += a[i] * b[i];
  }
  return d;
}

void main() {
  group('BlinkDetector', () {
    test('mở → nhắm → mở thì qua', () {
      final b = BlinkDetector();
      expect([0.9, 0.9, 0.1, 0.5, 0.9].map(b.update).toList().last, isTrue);
      expect(b.done, isTrue);
    });

    test('ảnh tĩnh (mắt luôn mở) không qua', () {
      final b = BlinkDetector();
      for (var i = 0; i < 100; i++) {
        b.update(0.95);
      }
      expect(b.done, isFalse);
    });

    test('mắt nhắm suốt không qua; không thấy mặt bị bỏ qua', () {
      final b = BlinkDetector();
      for (final v in [0.1, null, 0.1, 0.1]) {
        b.update(v);
      }
      expect(b.done, isFalse);
    });

    test('reset bắt đầu lại', () {
      final b = BlinkDetector()..update(0.9);
      b.reset();
      expect(b.hint, contains('mở to mắt'));
    });
  });

  group('embeddingFromPoints', () {
    final shape = [
      for (var i = 0; i < 36; i++)
        math.Point(100 + 40 * math.cos(i / 36 * 2 * math.pi),
            120 + 60 * math.sin(i / 36 * 2 * math.pi)),
    ];

    test('độ dài 1 và bất biến theo dịch/phóng', () {
      final a = embeddingFromPoints(shape, 0);
      final moved =
          shape.map((p) => math.Point(p.x * 2 + 50, p.y * 2 - 30)).toList();
      final b = embeddingFromPoints(moved, 0);
      expect(cosine(a, a), closeTo(1, 1e-9));
      expect(cosine(a, b), closeTo(1, 1e-9));
    });

    test('bù góc nghiêng đầu', () {
      final a = embeddingFromPoints(shape, 0);
      const deg = 20.0;
      final r = deg * math.pi / 180;
      final tilted = shape
          .map((p) => math.Point(p.x * math.cos(r) - p.y * math.sin(r),
              p.x * math.sin(r) + p.y * math.cos(r)))
          .toList();
      expect(cosine(a, embeddingFromPoints(tilted, deg)), closeTo(1, 1e-6));
    });
  });
}
