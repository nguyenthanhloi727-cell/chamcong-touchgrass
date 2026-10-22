import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/widgets/animations.dart';
import '../../core/widgets/ui.dart';
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
            tooltip: 'Hồ sơ',
            icon: const Icon(Icons.account_circle_outlined),
            onPressed: () => context.push('/profile'),
          ),
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
          FadeSlideIn(
            offset: -14,
            child: Container(
              width: double.infinity,
              margin: const EdgeInsets.fromLTRB(16, 4, 16, 8),
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(24),
                gradient: LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [scheme.primary, Color.lerp(scheme.primary, Colors.black, 0.28)!],
                ),
                boxShadow: [
                  BoxShadow(
                    color: scheme.primary.withValues(alpha: 0.28),
                    blurRadius: 18,
                    offset: const Offset(0, 8),
                  ),
                ],
              ),
              child: Row(
                children: [
                  InitialsAvatar(
                    name: u.fullName,
                    radius: 26,
                    background: Colors.white.withValues(alpha: 0.22),
                    foreground: Colors.white,
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(u.fullName,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: Theme.of(context)
                                .textTheme
                                .titleLarge
                                ?.copyWith(
                                    fontWeight: FontWeight.w700,
                                    color: scheme.onPrimary)),
                        const SizedBox(height: 2),
                        Text('${roleLabel(u.role)}$position',
                            key: const Key('role-position'),
                            style: TextStyle(
                                color: scheme.onPrimary.withValues(alpha: 0.85))),
                      ],
                    ),
                  ),
                ],
              ),
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
          return const SkeletonList(count: 5);
        }
        if (snap.hasError) {
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              EmptyState(
                icon: Icons.cloud_off_outlined,
                text:
                    'Không tải được dữ liệu. Kéo xuống để thử lại.\n${snap.error}',
              ),
            ],
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
