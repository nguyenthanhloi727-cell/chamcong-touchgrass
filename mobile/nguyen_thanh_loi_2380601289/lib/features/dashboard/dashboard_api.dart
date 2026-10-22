import '../../core/api/api_client.dart';
import 'dashboard_models.dart';

String _date(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

/// Các lệnh GET/POST mà 3 dashboard dùng chung.
class DashboardApi {
  final ApiClient api;
  DashboardApi(this.api);

  Future<List<AttendanceItem>> attendances({DateTime? from, DateTime? to}) async {
    final res = await api.dio.get('/api/attendances', queryParameters: {
      if (from != null) 'from': _date(from),
      if (to != null) 'to': _date(to),
    });
    return (res.data as List)
        .map((e) => AttendanceItem.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<List<EmployeeItem>> employees() async {
    final res = await api.dio.get('/api/employees');
    return (res.data as List)
        .map((e) => EmployeeItem.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<List<ShiftItem>> shifts() async {
    final res = await api.dio.get('/api/shifts');
    return (res.data as List)
        .map((e) => ShiftItem.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<void> review(int attendanceId, {required bool approve}) =>
      api.dio.post('/api/attendances/$attendanceId/review',
          data: {'approve': approve});
}
