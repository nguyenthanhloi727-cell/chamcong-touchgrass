import 'package:flutter/material.dart';

import '../auth/session_store.dart';
import 'dashboard_api.dart';
import 'dashboard_models.dart';
import 'dashboard_shell.dart';

/// Dashboard Nhân viên: nút Chấm công, ca hôm nay, lịch sử tháng, điểm tin cậy gần nhất.
class EmployeeDashboard extends StatefulWidget {
  final SessionStore session;
  const EmployeeDashboard({super.key, required this.session});

  @override
  State<EmployeeDashboard> createState() => _EmployeeDashboardState();
}

class _EmployeeDashboardState extends State<EmployeeDashboard> {
  late final DashboardApi _api = DashboardApi(widget.session.api);
  late Future<(List<ShiftItem>, List<AttendanceItem>)> _data = _load();

  Future<(List<ShiftItem>, List<AttendanceItem>)> _load() async {
    final now = DateTime.now();
    final r = await Future.wait([
      _api.shifts(),
      _api.attendances(from: DateTime(now.year, now.month, 1), to: now),
    ]);
    return (r[0] as List<ShiftItem>, r[1] as List<AttendanceItem>);
  }

  Future<void> _reload() async {
    final f = _load();
    setState(() {
      _data = f;
    });
    try {
      await f;
    } catch (_) {}
  }

  /// Ca đang diễn ra; không có thì ca bắt đầu sớm nhất.
  ShiftItem? _todayShift(List<ShiftItem> shifts) {
    if (shifts.isEmpty) return null;
    final now = '${hm(DateTime.now())}:00';
    for (final s in shifts) {
      if (s.start.compareTo(now) <= 0 && now.compareTo(s.end) <= 0) return s;
    }
    return ([...shifts]..sort((a, b) => a.start.compareTo(b.start))).first;
  }

  String _day(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}';

  @override
  Widget build(BuildContext context) {
    final text = Theme.of(context).textTheme;
    return DashboardShell(
      session: widget.session,
      title: 'Nhân viên',
      onRefresh: _reload,
      body: AsyncSection(
        future: _data,
        builder: (data) {
          final (shifts, history) = data;
          final shift = _todayShift(shifts);
          final last = history.isEmpty ? null : history.first; // API sắp mới → cũ
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              SizedBox(
                height: 96,
                child: FilledButton.icon(
                  onPressed: () => comingSoon(context, 'Chấm công', 14),
                  icon: const Icon(Icons.fingerprint, size: 40),
                  label:
                      const Text('Chấm công', style: TextStyle(fontSize: 24)),
                ),
              ),
              const SizedBox(height: 16),
              Card(
                child: ListTile(
                  leading: const Icon(Icons.schedule),
                  title: const Text('Ca hôm nay'),
                  subtitle: Text(shift?.label ?? 'Chưa có ca làm'),
                ),
              ),
              Card(
                child: ListTile(
                  leading: const Icon(Icons.verified_user),
                  title: const Text('Điểm tin cậy lần gần nhất'),
                  subtitle: Text(last == null
                      ? 'Chưa có lần chấm công nào'
                      : '${last.trustScore}/100 · ${statusLabel(last.status)}'),
                ),
              ),
              const SizedBox(height: 8),
              Text('Lịch sử chấm công tháng này', style: text.titleMedium),
              if (history.isEmpty)
                const ListTile(title: Text('Chưa có dữ liệu')),
              for (final a in history)
                ListTile(
                  leading: const Icon(Icons.event_available),
                  title: Text('${_day(a.checkInAt)} · ${hm(a.checkInAt)}'
                      '${a.checkOutAt != null ? ' - ${hm(a.checkOutAt!)}' : ''}'),
                  subtitle:
                      Text('${statusLabel(a.status)} · điểm ${a.trustScore}'),
                ),
            ],
          );
        },
      ),
    );
  }
}
