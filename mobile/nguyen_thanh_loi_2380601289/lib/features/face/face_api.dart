import '../../core/api/api_client.dart';

class FaceVerifyResult {
  final bool matched;
  final double score;
  final String? faceToken;
  const FaceVerifyResult(this.matched, this.score, this.faceToken);
}

class FaceApi {
  final ApiClient api;
  FaceApi(this.api);

  Future<void> enroll(List<double> embedding) =>
      api.dio.post('/api/face/enroll', data: {'embedding': embedding});

  Future<FaceVerifyResult> verify(List<double> embedding) async {
    final r =
        await api.dio.post('/api/face/verify', data: {'embedding': embedding});
    final d = r.data as Map<String, dynamic>;
    return FaceVerifyResult(d['matched'] as bool,
        (d['score'] as num).toDouble(), d['faceToken'] as String?);
  }
}
