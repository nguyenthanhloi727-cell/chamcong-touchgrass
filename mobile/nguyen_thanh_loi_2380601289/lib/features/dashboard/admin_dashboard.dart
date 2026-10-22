import 'package:flutter/material.dart';

import '../auth/session_store.dart';
import 'dashboard_shell.dart';

/// Dashboard Admin: lưới menu quản trị. Các màn đích làm ở Bước 13/14.
class AdminDashboard extends StatelessWidget {
  final SessionStore session;
  const AdminDashboard({super.key, required this.session});

  @override
  Widget build(BuildContext context) {
    final items = <(IconData, String, int)>[
      (Icons.people, 'Nhân viên', 13),
      (Icons.apartment, 'Phòng ban', 13),
      (Icons.badge, 'Chức vụ', 13),
      (Icons.schedule, 'Ca làm', 13),
      (Icons.fact_check, 'Chấm công', 13),
      (Icons.bar_chart, 'Báo cáo', 14),
      (Icons.qr_code_2, 'Màn QR', 14),
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
          for (final (icon, label, step) in items)
            Card(
              child: InkWell(
                borderRadius: BorderRadius.circular(12),
                onTap: () => comingSoon(context, label, step),
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
