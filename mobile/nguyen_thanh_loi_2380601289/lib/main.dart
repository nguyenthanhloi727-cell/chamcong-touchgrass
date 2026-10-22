import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import 'core/api/api_client.dart';
import 'core/api/token_storage.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';

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
  late final GoRouter _router = buildRouter(_api);

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
