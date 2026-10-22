import 'package:flutter/material.dart';

import '../../core/api/api_client.dart';
import '../auth/session_store.dart';
import 'face_api.dart';
import 'face_capture_page.dart';

/// Đăng ký khuôn mặt cho tài khoản đang đăng nhập (chớp mắt → embedding → enroll).
class FaceEnrollPage extends StatefulWidget {
  final SessionStore session;
  const FaceEnrollPage({super.key, required this.session});

  @override
  State<FaceEnrollPage> createState() => _FaceEnrollPageState();
}

class _FaceEnrollPageState extends State<FaceEnrollPage> {
  bool _busy = false;

  Future<void> _enroll() async {
    setState(() => _busy = true);
    try {
      final emb =
          await FaceCapturePage.open(context, title: 'Đăng ký khuôn mặt');
      if (emb == null) return;
      await FaceApi(widget.session.api).enroll(emb);
      await widget.session.restore();
      messengerKey.currentState?.showSnackBar(
          const SnackBar(content: Text('Đã đăng ký khuôn mặt')));
      if (mounted) Navigator.of(context).maybePop();
    } catch (e) {
      showError(e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final has = widget.session.user?.hasFace ?? false;
    return Scaffold(
      appBar: AppBar(title: const Text('Khuôn mặt')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(has ? 'Đã đăng ký khuôn mặt' : 'Chưa đăng ký khuôn mặt',
                style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            const Text(
                'Nhìn thẳng vào camera, chớp mắt một lần. Đăng ký lại sẽ ghi đè khuôn mặt cũ.'),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: _busy ? null : _enroll,
              icon: const Icon(Icons.face),
              label: Text(has ? 'Đăng ký lại' : 'Đăng ký khuôn mặt'),
            ),
          ],
        ),
      ),
    );
  }
}
