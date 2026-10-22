import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/api_client.dart';
import '../../core/widgets/animations.dart';
import '../../core/widgets/ui.dart';
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
    setState(() {
      _data = f;
    });
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
        onPressed: () => context.push('/manager/qr'),
        icon: const Icon(Icons.qr_code_2_rounded),
        label: const Text('Màn QR'),
      ),
      body: AsyncSection(
        future: _data,
        builder: (data) {
          final (employees, today) = data;
          final late = today.where((a) => a.isLate).toList();
          final pending = today.where((a) => a.isPending).toList();
          return ListView(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 96),
            children: [
              FadeSlideIn(
                child: Row(
                  children: [
                    Expanded(
                      child: _StatTile(
                          icon: Icons.fact_check_rounded,
                          color: const Color(0xFF16A34A),
                          value: today.length,
                          label: 'Lượt chấm công'),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: _StatTile(
                          icon: Icons.alarm_rounded,
                          color: const Color(0xFFEA580C),
                          value: late.length,
                          label: 'Đi muộn'),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: _StatTile(
                          icon: Icons.hourglass_top_rounded,
                          color: const Color(0xFF2563EB),
                          value: pending.length,
                          label: 'Chờ duyệt'),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 14),
              FadeSlideIn(
                index: 1,
                child: Row(
                  children: [
                    Expanded(
                      child: FilledButton.tonalIcon(
                        style: FilledButton.styleFrom(
                            minimumSize: const Size.fromHeight(48)),
                        icon: const Icon(Icons.rule_rounded),
                        label: const Text('Duyệt chi tiết'),
                        onPressed: () async {
                          await context.push('/manager/review');
                          _reload();
                        },
                      ),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: FilledButton.tonalIcon(
                        style: FilledButton.styleFrom(
                            minimumSize: const Size.fromHeight(48)),
                        icon: const Icon(Icons.location_searching_rounded),
                        label: const Text('Vị trí nhân viên'),
                        onPressed: () => context.push('/manager/team'),
                      ),
                    ),
                  ],
                ),
              ),
              const SectionTitle('Chờ duyệt'),
              if (pending.isEmpty)
                const EmptyState(
                    icon: Icons.task_alt_rounded, text: 'Không có'),
              for (var i = 0; i < pending.length; i++)
                FadeSlideIn(
                  index: i + 2,
                  child: Card(
                    child: ListTile(
                      leading: InitialsAvatar(name: pending[i].fullName),
                      title: Text(pending[i].fullName),
                      subtitle: Text('Điểm tin cậy ${pending[i].trustScore}'
                          '${pending[i].isMocked ? ' · GPS giả' : ''}'),
                      trailing: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          IconButton.filledTonal(
                            tooltip: 'Từ chối',
                            style: IconButton.styleFrom(
                              backgroundColor:
                                  const Color(0xFFDC2626).withValues(alpha: 0.14),
                              foregroundColor: const Color(0xFFDC2626),
                            ),
                            icon: const Icon(Icons.close_rounded),
                            onPressed: () => _review(pending[i], false),
                          ),
                          const SizedBox(width: 4),
                          IconButton.filledTonal(
                            tooltip: 'Duyệt',
                            style: IconButton.styleFrom(
                              backgroundColor:
                                  const Color(0xFF16A34A).withValues(alpha: 0.14),
                              foregroundColor: const Color(0xFF16A34A),
                            ),
                            icon: const Icon(Icons.check_rounded),
                            onPressed: () => _review(pending[i], true),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              const SectionTitle('Đi muộn hôm nay'),
              if (late.isEmpty)
                const EmptyState(
                    icon: Icons.sentiment_satisfied_alt_rounded,
                    text: 'Không có'),
              for (var i = 0; i < late.length; i++)
                FadeSlideIn(
                  index: i,
                  child: Card(
                    child: ListTile(
                      leading: const IconBadge(
                          icon: Icons.alarm_rounded, color: Color(0xFFEA580C)),
                      title: Text(late[i].fullName),
                      subtitle: Text('Vào lúc ${hm(late[i].checkInAt)}'),
                    ),
                  ),
                ),
              SectionTitle('Nhân viên phòng (${employees.length})'),
              for (var i = 0; i < employees.length; i++)
                FadeSlideIn(
                  index: i,
                  child: Card(
                    child: ListTile(
                      leading: InitialsAvatar(name: employees[i].fullName),
                      title: Text(employees[i].fullName),
                      subtitle: Text(
                          '${roleLabel(employees[i].role)}${employees[i].position != null ? ' · ${employees[i].position}' : ''}'),
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

class _StatTile extends StatelessWidget {
  final IconData icon;
  final Color color;
  final int value;
  final String label;
  const _StatTile({
    required this.icon,
    required this.color,
    required this.value,
    required this.label,
  });

  @override
  Widget build(BuildContext context) {
    final text = Theme.of(context).textTheme;
    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            IconBadge(icon: icon, color: color, size: 34),
            const SizedBox(height: 10),
            CountUp(
              value: value,
              style: text.headlineMedium
                  ?.copyWith(fontWeight: FontWeight.w800, color: color),
            ),
            Text(label,
                style: text.labelMedium?.copyWith(
                    color: Theme.of(context).colorScheme.onSurfaceVariant)),
          ],
        ),
      ),
    );
  }
}
