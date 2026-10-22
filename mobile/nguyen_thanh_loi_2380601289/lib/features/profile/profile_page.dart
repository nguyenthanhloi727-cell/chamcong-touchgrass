import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/api_client.dart';
import '../auth/auth_service.dart';
import '../auth/register_page.dart' show emailRe, phoneRe;
import '../auth/session_store.dart';

/// Hồ sơ: trạng thái Email / SĐT, bổ sung + xác nhận qua OTP, đăng ký khuôn mặt.
class ProfilePage extends StatelessWidget {
  final SessionStore session;
  final AuthService auth;
  const ProfilePage({super.key, required this.session, required this.auth});

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: session,
      builder: (context, _) {
        final u = session.user;
        if (u == null) return const SizedBox.shrink();
        return Scaffold(
          appBar: AppBar(title: const Text('Hồ sơ')),
          body: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text(u.fullName, style: Theme.of(context).textTheme.titleLarge),
              Text('@${u.username}'),
              const SizedBox(height: 16),
              _ContactTile(
                key: const Key('contact-email'),
                icon: Icons.email_outlined,
                label: 'Email',
                value: u.email,
                verified: u.emailVerified,
                auth: auth,
                type: 'email',
              ),
              _ContactTile(
                key: const Key('contact-phone'),
                icon: Icons.phone_outlined,
                label: 'Số điện thoại',
                value: u.phoneNumber,
                verified: u.phoneVerified,
                auth: auth,
                type: 'sms',
              ),
              const Divider(height: 32),
              ListTile(
                leading: const Icon(Icons.face),
                title: const Text('Khuôn mặt'),
                subtitle: Text(u.hasFace ? 'Đã đăng ký' : 'Chưa đăng ký'),
              ),
              FilledButton.icon(
                onPressed: () => context.push('/face-enroll'),
                icon: const Icon(Icons.face),
                label: Text(u.hasFace ? 'Đăng ký lại khuôn mặt' : 'Đăng ký khuôn mặt'),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _ContactTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final String? value;
  final bool verified;
  final AuthService auth;
  final String type; // "email" | "sms"
  const _ContactTile({
    super.key,
    required this.icon,
    required this.label,
    required this.value,
    required this.verified,
    required this.auth,
    required this.type,
  });

  bool get _hasValue => value != null && value!.isNotEmpty;

  String get _status =>
      !_hasValue ? 'Chưa có' : (verified ? 'Đã xác nhận' : 'Chưa xác nhận');

  Future<void> _edit(BuildContext context) async {
    final input = await showDialog<String>(
      context: context,
      builder: (_) => _ContactDialog(
        label: label,
        isEmail: type == 'email',
        initial: value ?? '',
      ),
    );
    if (input == null || !context.mounted) return;
    try {
      final otp = await auth.addContact(type, input);
      if (!context.mounted) return;
      await context.push<bool>('/profile/otp', extra: otp);
    } catch (e) {
      showError(e);
    }
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return ListTile(
      leading: Icon(icon),
      title: Text(label),
      subtitle: Text(_hasValue ? '$value\n$_status' : _status),
      isThreeLine: _hasValue,
      trailing: verified && _hasValue
          ? Icon(Icons.verified, color: scheme.primary)
          : OutlinedButton(
              onPressed: () => _edit(context),
              child: Text(_hasValue ? 'Xác nhận' : 'Thêm'),
            ),
    );
  }
}

class _ContactDialog extends StatefulWidget {
  final String label;
  final bool isEmail;
  final String initial;
  const _ContactDialog(
      {required this.label, required this.isEmail, required this.initial});

  @override
  State<_ContactDialog> createState() => _ContactDialogState();
}

class _ContactDialogState extends State<_ContactDialog> {
  late final _ctrl = TextEditingController(text: widget.initial);
  String? _error;

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  void _submit() {
    final v = _ctrl.text.trim();
    final ok = widget.isEmail ? emailRe.hasMatch(v) : phoneRe.hasMatch(v);
    if (!ok) {
      setState(() => _error =
          widget.isEmail ? 'Email không hợp lệ' : 'Số điện thoại không hợp lệ');
      return;
    }
    Navigator.of(context).pop(v);
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text('Thêm / xác nhận ${widget.label}'),
      content: TextField(
        controller: _ctrl,
        autofocus: true,
        keyboardType:
            widget.isEmail ? TextInputType.emailAddress : TextInputType.phone,
        decoration: InputDecoration(labelText: widget.label, errorText: _error),
        onSubmitted: (_) => _submit(),
      ),
      actions: [
        TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Hủy')),
        FilledButton(onPressed: _submit, child: const Text('Gửi mã OTP')),
      ],
    );
  }
}
