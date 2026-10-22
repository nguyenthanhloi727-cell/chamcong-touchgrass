class AttendanceItem {
  final int id;
  final int userId;
  final String username;
  final String fullName;
  final int? shiftId;
  final String? shiftName;
  final DateTime checkInAt;
  final DateTime? checkOutAt;
  final double lat;
  final double lng;
  final bool isMocked;
  final int trustScore;
  final String status; // OnTime | Late | PendingReview | Rejected

  const AttendanceItem({
    required this.id,
    required this.userId,
    required this.username,
    required this.fullName,
    this.shiftId,
    this.shiftName,
    required this.checkInAt,
    this.checkOutAt,
    this.lat = 0,
    this.lng = 0,
    required this.isMocked,
    required this.trustScore,
    required this.status,
  });

  factory AttendanceItem.fromJson(Map<String, dynamic> j) => AttendanceItem(
        id: j['id'] as int,
        userId: j['userId'] as int,
        username: j['username'] as String,
        fullName: j['fullName'] as String,
        shiftId: j['shiftId'] as int?,
        shiftName: j['shiftName'] as String?,
        checkInAt: DateTime.parse(j['checkInAt'] as String),
        checkOutAt: j['checkOutAt'] == null
            ? null
            : DateTime.parse(j['checkOutAt'] as String),
        lat: (j['lat'] as num?)?.toDouble() ?? 0,
        lng: (j['lng'] as num?)?.toDouble() ?? 0,
        isMocked: j['isMocked'] as bool? ?? false,
        trustScore: j['trustScore'] as int? ?? 0,
        status: j['status'] as String,
      );

  bool get isPending => status == 'PendingReview';
  bool get isLate => status == 'Late';
}

class EmployeeItem {
  final int id;
  final String username;
  final String fullName;
  final String role;
  final String? position;
  final String status;

  const EmployeeItem({
    required this.id,
    required this.username,
    required this.fullName,
    required this.role,
    this.position,
    required this.status,
  });

  factory EmployeeItem.fromJson(Map<String, dynamic> j) => EmployeeItem(
        id: j['id'] as int,
        username: j['username'] as String,
        fullName: j['fullName'] as String,
        role: j['role'] as String,
        position: j['position'] as String?,
        status: j['status'] as String,
      );
}

class ShiftItem {
  final int id;
  final String name;
  final String start; // "HH:mm:ss"
  final String end;

  const ShiftItem({
    required this.id,
    required this.name,
    required this.start,
    required this.end,
  });

  factory ShiftItem.fromJson(Map<String, dynamic> j) => ShiftItem(
        id: j['id'] as int,
        name: j['name'] as String,
        start: j['startTime'] as String,
        end: j['endTime'] as String,
      );

  String get label =>
      '$name (${start.substring(0, 5)} - ${end.substring(0, 5)})';
}

/// Tên hiển thị tiếng Việt của Role / trạng thái chấm công.
String roleLabel(String role) => switch (role) {
      'Admin' => 'Admin',
      'Manager' => 'Quản lý',
      _ => 'Nhân viên',
    };

String statusLabel(String status) => switch (status) {
      'OnTime' => 'Đúng giờ',
      'Late' => 'Đi muộn',
      'PendingReview' => 'Chờ duyệt',
      'Rejected' => 'Từ chối',
      _ => status,
    };

String hm(DateTime d) =>
    '${d.hour.toString().padLeft(2, '0')}:${d.minute.toString().padLeft(2, '0')}';
