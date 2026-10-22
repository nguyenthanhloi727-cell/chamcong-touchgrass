import 'package:flutter/material.dart';

import '../auth/session_store.dart';

/// Trang chủ tạm sau đăng nhập; Bước 12 thay bằng 3 dashboard theo role.
class HomePage extends StatelessWidget {
  final SessionStore session;
  const HomePage({super.key, required this.session});

  @override
  Widget build(BuildContext context) {
    final u = session.user!;
    final position = u.position != null ? ' · ${u.position}' : '';
    return Scaffold(
      appBar: AppBar(
        title: const Text('Chấm công'),
        actions: [
          IconButton(
            tooltip: 'Đăng xuất',
            icon: const Icon(Icons.logout),
            onPressed: session.logout,
          ),
        ],
      ),
      body: Center(
        child: Text(
          'Xin chào ${u.fullName}\n${u.role}$position',
          textAlign: TextAlign.center,
          style: Theme.of(context).textTheme.titleLarge,
        ),
      ),
    );
  }
}
