import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/widgets/animations.dart';
import '../../core/widgets/ui.dart';
import '../auth/session_store.dart';
import 'dashboard_shell.dart';

/// Dashboard Admin: lưới menu quản trị. Màn CRUD ở Bước 13, báo cáo/QR ở Bước 14.
class AdminDashboard extends StatelessWidget {
  final SessionStore session;
  const AdminDashboard({super.key, required this.session});

  @override
  Widget build(BuildContext context) {
    final items = <(IconData, String, String?, int, Color)>[
      (Icons.people_alt_rounded, 'Nhân viên', '/admin/employees', 13, const Color(0xFF2563EB)),
      (Icons.apartment_rounded, 'Phòng ban', '/admin/departments', 13, const Color(0xFF7C3AED)),
      (Icons.badge_rounded, 'Chức vụ', '/admin/positions', 13, const Color(0xFFDB2777)),
      (Icons.schedule_rounded, 'Ca làm', '/admin/shifts', 13, const Color(0xFFEA580C)),
      (Icons.fact_check_rounded, 'Chấm công', '/admin/attendances', 13, const Color(0xFF16A34A)),
      (Icons.bar_chart_rounded, 'Báo cáo', null, 14, const Color(0xFF0891B2)),
      (Icons.qr_code_2_rounded, 'Màn QR', '/admin/qr', 14, const Color(0xFF111827)),
      (Icons.rule_rounded, 'Duyệt chấm công', '/admin/review', 14, const Color(0xFFCA8A04)),
      (Icons.location_searching_rounded, 'Vị trí nhân viên', '/admin/team', 14, const Color(0xFFDC2626)),
    ];
    return DashboardShell(
      session: session,
      title: 'Quản trị',
      body: GridView.count(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
        crossAxisCount: 2,
        mainAxisSpacing: 14,
        crossAxisSpacing: 14,
        childAspectRatio: 1.08,
        children: [
          for (var i = 0; i < items.length; i++)
            FadeSlideIn(
              index: i,
              child: PressScale(
                child: _MenuTile(
                  icon: items[i].$1,
                  label: items[i].$2,
                  color: items[i].$5,
                  onTap: () => items[i].$3 != null
                      ? context.push(items[i].$3!)
                      : comingSoon(context, items[i].$2, items[i].$4),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _MenuTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final Color color;
  final VoidCallback onTap;
  const _MenuTile({
    required this.icon,
    required this.label,
    required this.color,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Card(
      margin: EdgeInsets.zero,
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        splashColor: color.withValues(alpha: 0.12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              IconBadge(icon: icon, color: color, size: 52),
              Row(
                children: [
                  Expanded(
                    child: Text(
                      label,
                      style: Theme.of(context)
                          .textTheme
                          .titleMedium
                          ?.copyWith(fontWeight: FontWeight.w700),
                    ),
                  ),
                  Icon(Icons.arrow_forward_rounded,
                      size: 18, color: scheme.onSurfaceVariant),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
