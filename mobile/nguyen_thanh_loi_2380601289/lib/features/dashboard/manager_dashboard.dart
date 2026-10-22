import 'package:flutter/material.dart';

import '../../core/api/api_client.dart';
import '../auth/session_store.dart';
import 'dashboard_api.dart';
import 'dashboard_models.dart';
import 'dashboard_shell.dart';

/// Dashboard Quản lý: nhân viên phòng mình + chấm công hôm nay (muộn / chờ duyệt).
class ManagerDashboard extends StatefulWidget {
  final SessionStore session;
  const ManagerDashboard({super.key, required this.session});

  @override
  State<ManagerDashboard> createState() => _ManagerDashboardState();
}

class _ManagerDashboardState extends State<ManagerDashboard> {
  late final DashboardApi _api = DashboardApi(widget.session.api);
  late Future<(List<EmployeeItem>, List<AttendanceItem>)> _data = _load();

  Future<(List<EmployeeItem>, List<AttendanceItem>)> _load() async {
    final today = DateTime.now();
    final r = await Future.wait([
      _api.employees(),
      _api.attendances(from: today, to: today),
    ]);
    return (r[0] as List<EmployeeItem>, r[1] as List<AttendanceItem>);
  }

  Future<void> _reload() async {
    final f = _load();
    setState(() => _data = f);
    try {
      await f;
    } catch (_) {}
  }

  Future<void> _review(AttendanceItem a, bool approve) async {
    try {
      await _api.review(a.id, approve: approve);
      await _reload();
    } catch (e) {
      showError(e);
    }
  }

  @override
  Widget build(BuildContext context) {
    return DashboardShell(
      session: widget.session,
      title: 'Quản lý',
      onRefresh: _reload,
      floating: FloatingActionButton.extended(
        onPressed: () => comingSoon(context, 'Màn QR', 14),
        icon: const Icon(Icons.qr_code_2),
        label: const Text('Màn QR'),
      ),
      body: AsyncSection(
        future: _data,
        builder: (data) {
          final (employees, today) = data;
          final late = today.where((a) => a.isLate).toList();
          final pending = today.where((a) => a.isPending).toList();
          final text = Theme.of(context).textTheme;
          return ListView(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 88),
            children: [
              Text('Hôm nay: ${today.length} lượt chấm công · '
                  '${late.length} đi muộn · ${pending.length} chờ duyệt'),
              const SizedBox(height: 16),
              Text('Chờ duyệt', style: text.titleMedium),
              if (pending.isEmpty) const ListTile(title: Text('Không có')),
              for (final a in pending)
                Card(
                  child: ListTile(
                    title: Text(a.fullName),
                    subtitle: Text('Điểm tin cậy ${a.trustScore}'
                        '${a.isMocked ? ' · GPS giả' : ''}'),
                    trailing: Wrap(children: [
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
                  ),
                ),
              const SizedBox(height: 16),
              Text('Đi muộn hôm nay', style: text.titleMedium),
              if (late.isEmpty) const ListTile(title: Text('Không có')),
              for (final a in late)
                ListTile(
                  leading: const Icon(Icons.alarm),
                  title: Text(a.fullName),
                  subtitle: Text('Vào lúc ${hm(a.checkInAt)}'),
                ),
              const SizedBox(height: 16),
              Text('Nhân viên phòng (${employees.length})',
                  style: text.titleMedium),
              for (final e in employees)
                ListTile(
                  leading: const Icon(Icons.person),
                  title: Text(e.fullName),
                  subtitle: Text(
                      '${roleLabel(e.role)}${e.position != null ? ' · ${e.position}' : ''}'),
                ),
            ],
          );
        },
      ),
    );
  }
}
