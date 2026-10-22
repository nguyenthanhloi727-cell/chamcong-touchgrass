import 'package:flutter/material.dart';

import '../../core/api/api_client.dart';
import '../../core/config.dart';

/// Màn kiểm tra kết nối backend (GET /api/health).
class HealthPage extends StatefulWidget {
  final ApiClient api;
  const HealthPage({super.key, required this.api});

  @override
  State<HealthPage> createState() => _HealthPageState();
}

class _HealthPageState extends State<HealthPage> {
  late Future<String> _health = widget.api.health();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Chấm công')),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(baseUrl, style: Theme.of(context).textTheme.bodySmall),
              const SizedBox(height: 16),
              FutureBuilder<String>(
                future: _health,
                builder: (context, snap) {
                  if (snap.connectionState != ConnectionState.done) {
                    return const CircularProgressIndicator();
                  }
                  if (snap.hasError) {
                    return Text(
                      apiErrorMessage(snap.error!),
                      style: TextStyle(color: Theme.of(context).colorScheme.error),
                      textAlign: TextAlign.center,
                    );
                  }
                  return Text(
                    'Health: ${snap.data}',
                    style: Theme.of(context).textTheme.headlineMedium,
                  );
                },
              ),
              const SizedBox(height: 24),
              FilledButton(
                onPressed: () => setState(() => _health = widget.api.health()),
                child: const Text('Thử lại'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
