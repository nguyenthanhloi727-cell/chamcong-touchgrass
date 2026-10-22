import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/api/api_client.dart';
import '../dashboard/dashboard_api.dart';
import '../dashboard/dashboard_models.dart';
import 'attendance_api.dart';
import 'geo_service.dart';

Future<void> openMaps(double lat, double lng) async {
  final uri = Uri.parse('https://www.google.com/maps/search/?api=1&query=$lat,$lng');
  if (!await launchUrl(uri, mode: LaunchMode.externalApplication)) {
    showError('Không mở được Google Maps');
  }
}

/// Duyệt chấm công `PendingReview`: vị trí đã báo, khoảng cách tới chi nhánh, nhãn GPS giả.
class ReviewPage extends StatefulWidget {
  final ApiClient api;
  const ReviewPage({super.key, required this.api});

  @override
  State<ReviewPage> createState() => _ReviewPageState();
}

class _ReviewPageState extends State<ReviewPage> {
  late final DashboardApi _dash = DashboardApi(widget.api);
  late final AttendanceApi _att = AttendanceApi(widget.api);
  late Future<(List<AttendanceItem>, List<BranchPoint>)> _data = _load();

  Future<(List<AttendanceItem>, List<BranchPoint>)> _load() async {
    final r = await Future.wait([
      _dash.attendances(
          from: DateTime.now().subtract(const Duration(days: 30)),
          to: DateTime.now()),
      _att.locations(),
    ]);
    final pending =
        (r[0] as List<AttendanceItem>).where((a) => a.isPending).toList();
    return (pending, r[1] as List<BranchPoint>);
  }

  Future<void> _review(AttendanceItem a, bool approve) async {
    try {
      await _dash.review(a.id, approve: approve);
      setState(() => _data = _load());
    } catch (e) {
      showError(e);
    }
  }

  String _nearest(AttendanceItem a, List<BranchPoint> branches) {
    if (branches.isEmpty) return 'Chưa có điểm chấm công';
    var best = branches.first;
    var d = distanceMeters(a.lat, a.lng, best.lat, best.lng);
    for (final b in branches.skip(1)) {
      final x = distanceMeters(a.lat, a.lng, b.lat, b.lng);
      if (x < d) {
        d = x;
        best = b;
      }
    }
    return 'Cách ${best.name} ${d.toStringAsFixed(0)} m (bán kính ${best.radius} m)';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Duyệt chấm công')),
      body: FutureBuilder(
        future: _data,
        builder: (context, snap) {
          if (snap.connectionState != ConnectionState.done) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snap.hasError) {
            return Center(child: Text(apiErrorMessage(snap.error!)));
          }
          final (items, branches) = snap.data!;
          if (items.isEmpty) {
            return const Center(child: Text('Không có bản ghi chờ duyệt'));
          }
          return ListView(
            children: [
              for (final a in items)
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('${a.fullName} · ${hm(a.checkInAt)} '
                            '${a.checkInAt.day}/${a.checkInAt.month}'),
                        Text('Điểm tin cậy ${a.trustScore}'),
                        if (a.isMocked)
                          const Text('GPS giả / emulator',
                              style: TextStyle(
                                  color: Colors.red,
                                  fontWeight: FontWeight.bold)),
                        Text(
                            'Vị trí đã báo: ${a.lat.toStringAsFixed(6)}, ${a.lng.toStringAsFixed(6)}'),
                        Text(_nearest(a, branches)),
                        Row(children: [
                          TextButton.icon(
                              onPressed: () => openMaps(a.lat, a.lng),
                              icon: const Icon(Icons.map),
                              label: const Text('Google Maps')),
                          const Spacer(),
                          IconButton(
                            tooltip: 'Từ chối',
                            icon: const Icon(Icons.close, color: Colors.red),
                            onPressed: () => _review(a, false),
                          ),
                          IconButton(
                            tooltip: 'Duyệt',
                            icon: const Icon(Icons.check, color: Colors.green),
                            onPressed: () => _review(a, true),
                          ),
                        ]),
                      ],
                    ),
                  ),
                ),
            ],
          );
        },
      ),
    );
  }
}
