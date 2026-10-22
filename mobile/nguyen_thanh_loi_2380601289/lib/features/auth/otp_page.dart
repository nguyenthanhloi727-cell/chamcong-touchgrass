import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/api_client.dart';
import 'auth_models.dart';
import 'auth_service.dart';

/// Màn OTP dùng chung: đăng ký / đăng nhập / bổ sung liên hệ.
class OtpPage extends StatefulWidget {
  final AuthService auth;
  final OtpSession session;

  const OtpPage({super.key, required this.auth, required this.session});

  @override
  State<OtpPage> createState() => _OtpPageState();
}

class _OtpPageState extends State<OtpPage> {
  static const _resendSeconds = 60;

  final _code = TextEditingController();
  late OtpSession _session = widget.session;
  Timer? _timer;
  int _left = _resendSeconds;
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _startCountdown();
  }

  @override
  void dispose() {
    _timer?.cancel();
    _code.dispose();
    super.dispose();
  }

  void _startCountdown() {
    _timer?.cancel();
    setState(() => _left = _resendSeconds);
    _timer = Timer.periodic(const Duration(seconds: 1), (t) {
      if (_left <= 1) t.cancel();
      setState(() => _left = _left > 0 ? _left - 1 : 0);
    });
  }

  Future<void> _verify() async {
    if (_busy) return;
    if (_code.text.length != 6) {
      setState(() => _error = 'Nhập đủ 6 số');
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final purpose = await widget.auth.verifyOtp(_session.id, _code.text);
      if (!mounted) return;
      // Register/Login: SessionStore đã có user → router tự chuyển màn.
      if (purpose == 'AddContact' && context.canPop()) context.pop(true);
    } catch (e) {
      if (mounted) setState(() => _error = apiErrorMessage(e));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _resend() async {
    if (_busy || _left > 0) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final next = await widget.auth.resendOtp(_session.id);
      if (!mounted) return;
      _code.clear();
      setState(() => _session = next);
      _startCountdown();
    } catch (e) {
      if (mounted) setState(() => _error = apiErrorMessage(e));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Nhập mã OTP')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            Text(
              'Mã đã gửi tới ${_session.maskedDestination} qua ${_session.channelLabel}',
              style: theme.textTheme.bodyLarge,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 24),
            TextField(
              controller: _code,
              enabled: !_busy,
              autofocus: true,
              keyboardType: TextInputType.number,
              textAlign: TextAlign.center,
              maxLength: 6,
              style: theme.textTheme.headlineMedium?.copyWith(letterSpacing: 12),
              inputFormatters: [FilteringTextInputFormatter.digitsOnly],
              decoration: InputDecoration(
                hintText: '------',
                counterText: '',
                errorText: _error,
                errorMaxLines: 3,
              ),
              onChanged: (v) {
                if (_error != null) setState(() => _error = null);
                if (v.length == 6) _verify();
              },
              onSubmitted: (_) => _verify(),
            ),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: _busy ? null : _verify,
              child: _busy
                  ? const SizedBox(
                      height: 20,
                      width: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Xác nhận'),
            ),
            const SizedBox(height: 8),
            TextButton(
              onPressed: (_busy || _left > 0) ? null : _resend,
              child: Text(_left > 0 ? 'Gửi lại sau ${_left}s' : 'Gửi lại mã'),
            ),
          ],
        ),
      ),
    );
  }
}
