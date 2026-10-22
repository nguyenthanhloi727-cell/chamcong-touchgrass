import 'package:flutter/material.dart';

import '../auth/session_store.dart';
import 'dashboard_models.dart';

/// Khung chung của 3 dashboard: header "Tên · Role · Chức vụ" + nút Đăng xuất.
class DashboardShell extends StatelessWidget {
  final SessionStore session;
  final String title;
  final Widget body;
  final Future<void> Function()? onRefresh;
  final Widget? floating;

  const DashboardShell({
    super.key,
    required this.session,
    required this.title,
    required this.body,
    this.onRefresh,
    this.floating,
  });

  @override
  Widget build(BuildContext context) {
    final u = session.user!;
    final position = (u.position != null && u.position!.isNotEmpty)
        ? ' · ${u.position}'
        : '';
    final scheme = Theme.of(context).colorScheme;
    Widget content = body;
    if (onRefresh != null) {
      content = RefreshIndicator(onRefresh: onRefresh!, child: body);
    }
    return Scaffold(
      appBar: AppBar(
        title: Text(title),
        actions: [
          IconButton(
            tooltip: 'Đăng xuất',
            icon: const Icon(Icons.logout),
            onPressed: session.logout,
          ),
        ],
      ),
      floatingActionButton: floating,
      body: Column(
        children: [
          Container(
            width: double.infinity,
            color: scheme.primaryContainer,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(u.fullName,
                    style: Theme.of(context)
                        .textTheme
                        .titleMedium
                        ?.copyWith(fontWeight: FontWeight.bold)),
                Text('${roleLabel(u.role)}$position',
                    key: const Key('role-position')),
              ],
            ),
          ),
          Expanded(child: content),
        ],
      ),
    );
  }
}

/// Hiện vòng quay / lỗi / nội dung cho một Future; dùng trong dashboard.
/// Luôn là ListView để kéo-làm-mới hoạt động cả khi đang tải hoặc lỗi.
class AsyncSection<T> extends StatelessWidget {
  final Future<T> future;
  final Widget Function(T data) builder;
  const AsyncSection({super.key, required this.future, required this.builder});

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<T>(
      future: future,
      builder: (context, snap) {
        if (snap.connectionState != ConnectionState.done) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snap.hasError) {
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [Text('Không tải được dữ liệu. Kéo xuống để thử lại.\n${snap.error}')],
          );
        }
        return builder(snap.data as T);
      },
    );
  }
}

void comingSoon(BuildContext context, String what, int step) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text('$what: làm ở Bước $step')));
}
