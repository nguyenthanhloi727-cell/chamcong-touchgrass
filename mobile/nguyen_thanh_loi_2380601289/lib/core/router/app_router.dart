import 'package:go_router/go_router.dart';

import '../api/api_client.dart';
import '../../features/dashboard/health_page.dart';

/// Router tạm của Bước 10; Bước 11–12 thêm route đăng nhập và route theo role.
GoRouter buildRouter(ApiClient api) {
  return GoRouter(
    routes: [
      GoRoute(
        path: '/',
        builder: (context, state) => HealthPage(api: api),
      ),
    ],
  );
}
