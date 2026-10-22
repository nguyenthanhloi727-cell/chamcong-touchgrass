import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import 'core/api/api_client.dart';
import 'core/api/token_storage.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/auth_service.dart';
import 'features/auth/session_store.dart';

void main() {
  runApp(const App());
}

class App extends StatefulWidget {
  const App({super.key});

  @override
  State<App> createState() => _AppState();
}

class _AppState extends State<App> {
  late final ApiClient _api = ApiClient(TokenStorage());
  late final SessionStore _session = SessionStore(_api)..restore();
  late final AuthService _auth = AuthService(_api, _session);
  late final GoRouter _router = buildRouter(_session, _auth);

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'Chấm công',
      theme: buildAppTheme(),
      scaffoldMessengerKey: messengerKey,
      routerConfig: _router,
    );
  }
}
