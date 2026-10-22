import 'dart:math' as math;

import 'package:google_mlkit_face_detection/google_mlkit_face_detection.dart';

/// Các đường viền ML Kit dùng để tạo vector, theo thứ tự cố định.
const _contourTypes = [
  FaceContourType.face,
  FaceContourType.leftEyebrowTop,
  FaceContourType.leftEyebrowBottom,
  FaceContourType.rightEyebrowTop,
  FaceContourType.rightEyebrowBottom,
  FaceContourType.leftEye,
  FaceContourType.rightEye,
  FaceContourType.upperLipTop,
  FaceContourType.upperLipBottom,
  FaceContourType.lowerLipTop,
  FaceContourType.lowerLipBottom,
  FaceContourType.noseBridge,
  FaceContourType.noseBottom,
];

/// Lấy các điểm viền của một khuôn mặt; null nếu thiếu đường viền nào.
List<math.Point<double>>? contourPoints(Face face) {
  final pts = <math.Point<double>>[];
  for (final t in _contourTypes) {
    final c = face.contours[t]?.points;
    if (c == null || c.isEmpty) return null;
    pts.addAll(c.map((p) => math.Point(p.x.toDouble(), p.y.toDouble())));
  }
  return pts;
}

/// Embedding dự phòng (không dùng MobileFaceNet): hình dạng các đường viền của
/// ML Kit, xoay về thẳng đứng theo góc nghiêng đầu, dời về tâm, chia kích thước
/// khuôn mặt rồi chuẩn hóa độ dài 1 → so cosine ở backend.
List<double> embeddingFromPoints(
    List<math.Point<double>> points, double rollDegrees) {
  final n = points.length;
  var cx = 0.0, cy = 0.0;
  for (final p in points) {
    cx += p.x;
    cy += p.y;
  }
  cx /= n;
  cy /= n;
  final a = -rollDegrees * math.pi / 180;
  final cosA = math.cos(a), sinA = math.sin(a);
  final xs = <double>[], ys = <double>[];
  for (final p in points) {
    final dx = p.x - cx, dy = p.y - cy;
    xs.add(dx * cosA - dy * sinA);
    ys.add(dx * sinA + dy * cosA);
  }
  final w = xs.reduce(math.max) - xs.reduce(math.min);
  final h = ys.reduce(math.max) - ys.reduce(math.min);
  final scale = math.max(w, h);
  final v = <double>[];
  for (var i = 0; i < n; i++) {
    v
      ..add(xs[i] / scale)
      ..add(ys[i] / scale);
  }
  var norm = 0.0;
  for (final x in v) {
    norm += x * x;
  }
  norm = math.sqrt(norm);
  return norm == 0 ? v : v.map((x) => x / norm).toList();
}

/// Embedding của một Face, hoặc null nếu thiếu đường viền.
List<double>? faceEmbedding(Face face) {
  final pts = contourPoints(face);
  if (pts == null) return null;
  return embeddingFromPoints(pts, face.headEulerAngleZ ?? 0);
}

/// Xác suất mở mắt trung bình hai mắt; null nếu ML Kit không trả.
double? eyesOpenProbability(Face face) {
  final l = face.leftEyeOpenProbability, r = face.rightEyeOpenProbability;
  if (l == null || r == null) return null;
  return (l + r) / 2;
}
