import 'package:flutter/material.dart';

import '../../core/api/api_client.dart';
import '../auth/auth_service.dart';
import 'face_capture_page.dart';

/// Đăng nhập bằng khuôn mặt: nhập username → chớp mắt → embedding → /api/face/login.
class FaceLoginPage extends StatefulWidget {
  final AuthService auth;
  const FaceLoginPage({super.key, required this.auth});

  @override
  State<FaceLoginPage> createState() => _FaceLoginPageState();
}

class _FaceLoginPageState extends State<FaceLoginPage> {
  final _form = GlobalKey<FormState>();
  final _username = TextEditingController();
  bool _busy = false;

  @override
  void dispose() {
    _username.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    if (!_form.currentState!.validate()) return;
    setState(() => _busy = true);
    try {
      final emb =
          await FaceCapturePage.open(context, title: 'Đăng nhập khuôn mặt');
      if (emb == null) return;
      await widget.auth.loginWithFace(_username.text.trim(), emb);
    } catch (e) {
      showError(e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Đăng nhập bằng khuôn mặt')),
      body: Form(
        key: _form,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            TextFormField(
              controller: _username,
              decoration: const InputDecoration(labelText: 'Tên đăng nhập'),
              validator: (v) =>
                  (v == null || v.trim().isEmpty) ? 'Nhập tên đăng nhập' : null,
            ),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: _busy ? null : _login,
              icon: const Icon(Icons.face),
              label: const Text('Chớp mắt để đăng nhập'),
            ),
          ],
        ),
      ),
    );
  }
}
