import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/auth_models.dart';
import '../../features/auth/auth_service.dart';
import '../../features/auth/login_page.dart';
import '../../features/auth/otp_page.dart';
import '../../features/auth/register_page.dart';
import '../../features/auth/session_store.dart';
import '../../features/dashboard/home_page.dart';
import '../../features/face/face_login_page.dart';

/// Bước 12 thêm route theo role; hiện tại: chưa đăng nhập → /login, đã đăng nhập → /.
GoRouter buildRouter(SessionStore session, AuthService auth) {
  const publicPaths = {'/login', '/register', '/otp', '/face-login'};
  return GoRouter(
    refreshListenable: session,
    redirect: (context, state) {
      if (session.restoring) return null;
      final isPublic = publicPaths.contains(state.matchedLocation);
      if (!session.isLoggedIn && !isPublic) return '/login';
      if (session.isLoggedIn && isPublic) return '/';
      return null;
    },
    routes: [
      GoRoute(
        path: '/',
        builder: (context, state) => session.restoring
            ? const Scaffold(body: Center(child: CircularProgressIndicator()))
            : HomePage(session: session),
      ),
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
