import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../auth/session_store.dart';
import 'dashboard_shell.dart';

/// Dashboard Admin: lưới menu quản trị. Màn CRUD ở Bước 13, báo cáo/QR ở Bước 14.
class AdminDashboard extends StatelessWidget {
  final SessionStore session;
  const AdminDashboard({super.key, required this.session});

  @override
  Widget build(BuildContext context) {
    final items = <(IconData, String, String?, int)>[
      (Icons.people, 'Nhân viên', '/admin/employees', 13),
      (Icons.apartment, 'Phòng ban', '/admin/departments', 13),
      (Icons.badge, 'Chức vụ', '/admin/positions', 13),
      (Icons.schedule, 'Ca làm', '/admin/shifts', 13),
      (Icons.fact_check, 'Chấm công', '/admin/attendances', 13),
      (Icons.bar_chart, 'Báo cáo', null, 14),
      (Icons.qr_code_2, 'Màn QR', '/admin/qr', 14),
      (Icons.rule, 'Duyệt chấm công', '/admin/review', 14),
      (Icons.location_searching, 'Vị trí nhân viên', '/admin/team', 14),
    ];
    return DashboardShell(
      session: session,
      title: 'Quản trị',
      body: GridView.count(
        padding: const EdgeInsets.all(16),
        crossAxisCount: 2,
        mainAxisSpacing: 12,
        crossAxisSpacing: 12,
        children: [
          for (final (icon, label, path, step) in items)
            Card(
              child: InkWell(
                borderRadius: BorderRadius.circular(12),
                onTap: () => path != null
                    ? context.push(path)
                    : comingSoon(context, label, step),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(icon, size: 40),
                    const SizedBox(height: 8),
                    Text(label, style: Theme.of(context).textTheme.titleMedium),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }
}
