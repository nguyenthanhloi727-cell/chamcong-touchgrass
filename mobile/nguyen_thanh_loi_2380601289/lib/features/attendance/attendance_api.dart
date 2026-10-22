import 'package:latlong2/latlong.dart';

import '../../core/api/api_client.dart';

class QrInfo {
  final String token;
  final int expiresIn;
  const QrInfo(this.token, this.expiresIn);
}

class BranchPoint {
  final int id;
  final String name;
  final double lat;
  final double lng;
  final int radius;
  const BranchPoint(this.id, this.name, this.lat, this.lng, this.radius);
  LatLng get point => LatLng(lat, lng);
}

class CheckInResult {
  final int trustScore;
  final int gps;
  final int qr;
  final int notMocked;
  final String status;
  final double distance;
  const CheckInResult({
    required this.trustScore,
    required this.gps,
    required this.qr,
    required this.notMocked,
    required this.status,
    required this.distance,
  });

  factory CheckInResult.fromJson(Map<String, dynamic> j) {
    final b = j['breakdown'] as Map<String, dynamic>;
    return CheckInResult(
      trustScore: j['trustScore'] as int,
      gps: b['gps'] as int,
      qr: b['qr'] as int,
      notMocked: b['notMocked'] as int,
      status: j['status'] as String,
      distance: (j['distanceMeters'] as num).toDouble(),
    );
  }
}

class TeamLocation {
  final String fullName;
  final String? position;
  final double lat;
  final double lng;
  final DateTime updatedAt;
  const TeamLocation(
      this.fullName, this.position, this.lat, this.lng, this.updatedAt);
}

class AttendanceApi {
  final ApiClient api;
  AttendanceApi(this.api);

  Future<QrInfo> qr() async {
    final r = await api.dio.get('/api/qr/current');
    return QrInfo(r.data['token'] as String, r.data['expiresIn'] as int);
  }

  Future<List<BranchPoint>> locations() async {
    final r = await api.dio.get('/api/locations');
    return (r.data as List)
        .cast<Map<String, dynamic>>()
        .map((j) => BranchPoint(
            j['id'] as int,
            j['name'] as String,
            (j['lat'] as num).toDouble(),
            (j['lng'] as num).toDouble(),
            j['radiusMeters'] as int))
        .toList();
  }

  Future<void> createLocation(String name, double lat, double lng,
          {int radius = 200}) =>
      api.dio.post('/api/locations',
          data: {'name': name, 'lat': lat, 'lng': lng, 'radiusMeters': radius});

  Future<CheckInResult> checkIn({
    required double lat,
    required double lng,
    required bool isMocked,
    String? qrToken,
  }) async {
    final r = await api.dio.post('/api/attendance/check-in', data: {
      'lat': lat,
      'lng': lng,
      'isMocked': isMocked,
      'qrToken': qrToken,
    });
    return CheckInResult.fromJson(r.data as Map<String, dynamic>);
  }

  Future<void> checkOut() => api.dio.post('/api/attendance/check-out');

  Future<void> shareLocation(double lat, double lng) =>
      api.dio.post('/api/location/share', data: {'lat': lat, 'lng': lng});

  Future<List<TeamLocation>> team() async {
    final r = await api.dio.get('/api/location/team');
    return (r.data as List)
        .cast<Map<String, dynamic>>()
        .map((j) => TeamLocation(
            j['fullName'] as String,
            j['positionName'] as String?,
            (j['lat'] as num).toDouble(),
            (j['lng'] as num).toDouble(),
            DateTime.parse(j['updatedAt'] as String)))
        .toList();
  }
}
