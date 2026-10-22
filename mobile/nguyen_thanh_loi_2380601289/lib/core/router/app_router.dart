import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/auth_models.dart';
import '../../features/auth/auth_service.dart';
import '../../features/auth/login_page.dart';
import '../../features/auth/otp_page.dart';
import '../../features/auth/register_page.dart';
import '../../features/auth/session_store.dart';
import '../../features/dashboard/admin_dashboard.dart';
import '../../features/dashboard/employee_dashboard.dart';
import '../../features/dashboard/manager_dashboard.dart';
import '../../features/face/face_login_page.dart';

/// Màn chính theo role: Admin → /admin, Manager → /manager, còn lại → /employee.
String homeForRole(String role) => switch (role) {
      'Admin' => '/admin',
      'Manager' => '/manager',
      _ => '/employee',
    };

const _roleRoots = {'/admin', '/manager', '/employee'};

/// Chưa đăng nhập → /login; đã đăng nhập → màn của role, chặn vào màn role khác.
GoRouter buildRouter(SessionStore session, AuthService auth) {
  const publicPaths = {'/login', '/register', '/otp', '/face-login'};
  return GoRouter(
    refreshListenable: session,
    redirect: (context, state) {
      if (session.restoring) return null;
      final isPublic = publicPaths.contains(state.matchedLocation);
      if (!session.isLoggedIn && !isPublic) return '/login';
      final user = session.user;
      if (user != null) {
        final home = homeForRole(user.role);
        if (isPublic || state.matchedLocation == '/') return home;
        final root = '/${state.matchedLocation.split('/')[1]}';
        if (_roleRoots.contains(root) && root != home) return home;
      }
      return null;
    },
    routes: [
      GoRoute(
        path: '/',
        builder: (_, _) =>
            const Scaffold(body: Center(child: CircularProgressIndicator())),
      ),
      GoRoute(
          path: '/admin', builder: (_, _) => AdminDashboard(session: session)),
      GoRoute(
          path: '/manager',
          builder: (_, _) => ManagerDashboard(session: session)),
      GoRoute(
          path: '/employee',
          builder: (_, _) => EmployeeDashboard(session: session)),
      GoRoute(path: '/login', builder: (_, _) => LoginPage(auth: auth)),
      GoRoute(path: '/register', builder: (_, _) => RegisterPage(auth: auth)),
      GoRoute(
        path: '/otp',
        builder: (context, state) {
          final otp = state.extra;
          // Mở lại app trên /otp (mất extra) → về đăng nhập.
          if (otp is! OtpSession) return LoginPage(auth: auth);
          return OtpPage(auth: auth, session: otp);
        },
      ),
      GoRoute(path: '/face-login', builder: (_, _) => const FaceLoginPage()),
    ],
  );
}
