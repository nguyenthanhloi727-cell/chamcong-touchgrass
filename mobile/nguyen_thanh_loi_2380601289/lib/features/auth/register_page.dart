import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/api_client.dart';
import 'auth_service.dart';

final _emailRe = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
final _phoneRe = RegExp(r'^(0\d{9}|\+84\d{9})$');

class RegisterPage extends StatefulWidget {
  final AuthService auth;
  const RegisterPage({super.key, required this.auth});

  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final _form = GlobalKey<FormState>();
  final _username = TextEditingController();
  final _fullName = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();
  final _email = TextEditingController();
  final _phone = TextEditingController();
  bool _busy = false;
  bool _hide = true;

  @override
  void initState() {
    super.initState();
    // Đổi Email/SĐT → bật/tắt nút xác nhận và kiểm lại lỗi 2 ô liên hệ.
    for (final c in [_email, _phone]) {
      c.addListener(() => setState(() {}));
    }
  }

  @override
  void dispose() {
    for (final c in [_username, _fullName, _password, _confirm, _email, _phone]) {
      c.dispose();
    }
    super.dispose();
  }

  String get _emailText => _email.text.trim();
  String get _phoneText => _phone.text.trim().replaceAll(' ', '');
  bool get _emailOk => _emailRe.hasMatch(_emailText);
  bool get _phoneOk => _phoneRe.hasMatch(_phoneText);

  String? _bothEmpty() => (_emailText.isEmpty && _phoneText.isEmpty)
      ? 'Nhập ít nhất Email hoặc Số điện thoại'
      : null;

  Future<void> _submit(String channel) async {
    if (_busy || !_form.currentState!.validate()) return;
    setState(() => _busy = true);
    try {
      final otp = await widget.auth.register(
        username: _username.text.trim(),
        password: _password.text,
        fullName: _fullName.text.trim(),
        email: _emailText.isEmpty ? null : _emailText,
        phone: _phoneText.isEmpty ? null : _phoneText,
        verifyChannel: channel,
      );
      if (mounted) context.push('/otp', extra: otp);
    } catch (e) {
      showError(e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Widget _label(String text) => _busy
      ? const SizedBox(
          height: 20,
          width: 20,
          child: CircularProgressIndicator(strokeWidth: 2),
        )
      : Text(text);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Đăng ký')),
      body: SafeArea(
        child: Form(
          key: _form,
          autovalidateMode: AutovalidateMode.onUserInteraction,
          child: ListView(
            padding: const EdgeInsets.all(24),
            children: [
              TextFormField(
                controller: _username,
                enabled: !_busy,
                autocorrect: false,
                textInputAction: TextInputAction.next,
                decoration: const InputDecoration(labelText: 'Username'),
                validator: (v) {
                  final s = v?.trim() ?? '';
                  if (s.length < 3) return 'Ít nhất 3 ký tự';
                  if (!RegExp(r'^[A-Za-z0-9_.]+$').hasMatch(s)) {
                    return 'Chỉ gồm chữ, số, dấu _ và .';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _fullName,
                enabled: !_busy,
                textCapitalization: TextCapitalization.words,
                textInputAction: TextInputAction.next,
                decoration: const InputDecoration(labelText: 'Họ tên'),
                validator: (v) =>
                    (v == null || v.trim().isEmpty) ? 'Nhập họ tên' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _password,
                enabled: !_busy,
                obscureText: _hide,
                textInputAction: TextInputAction.next,
                decoration: InputDecoration(
                  labelText: 'Mật khẩu',
                  suffixIcon: IconButton(
                    icon: Icon(_hide ? Icons.visibility : Icons.visibility_off),
                    onPressed: () => setState(() => _hide = !_hide),
                  ),
                ),
                validator: (v) =>
                    (v == null || v.length < 6) ? 'Ít nhất 6 ký tự' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _confirm,
                enabled: !_busy,
                obscureText: _hide,
                textInputAction: TextInputAction.next,
                decoration:
                    const InputDecoration(labelText: 'Nhập lại mật khẩu'),
                validator: (v) =>
                    v != _password.text ? 'Mật khẩu không khớp' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _email,
                enabled: !_busy,
                keyboardType: TextInputType.emailAddress,
                autocorrect: false,
                textInputAction: TextInputAction.next,
                decoration: const InputDecoration(labelText: 'Email'),
                validator: (_) {
                  final both = _bothEmpty();
                  if (both != null) return both;
                  if (_emailText.isNotEmpty && !_emailOk) {
                    return 'Email không hợp lệ';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _phone,
                enabled: !_busy,
                keyboardType: TextInputType.phone,
                decoration: const InputDecoration(
                  labelText: 'Số điện thoại',
                  hintText: '0xxxxxxxxx hoặc +84xxxxxxxxx',
                ),
                validator: (_) {
                  final both = _bothEmpty();
                  if (both != null) return both;
                  if (_phoneText.isNotEmpty && !_phoneOk) {
                    return 'Số điện thoại không hợp lệ';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 8),
              Text(
                'Được bỏ trống 1 trong 2 ô, không được trống cả hai.',
                style: Theme.of(context).textTheme.bodySmall,
              ),
              const SizedBox(height: 24),
              FilledButton.icon(
                onPressed: (_busy || !_emailOk) ? null : () => _submit('email'),
                icon: const Icon(Icons.email),
                label: _label('Xác nhận qua Email'),
              ),
              const SizedBox(height: 12),
              FilledButton.icon(
                onPressed: (_busy || !_phoneOk) ? null : () => _submit('sms'),
                icon: const Icon(Icons.sms),
                label: _label('Xác nhận qua SMS'),
              ),
              const SizedBox(height: 12),
              TextButton(
                onPressed: _busy ? null : () => context.pop(),
                child: const Text('Đã có tài khoản? Đăng nhập'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
