import 'package:flutter/material.dart';

import '../../core/api/api_client.dart';
import '../../core/widgets/animations.dart';
import '../../core/widgets/ui.dart';
import '../auth/session_store.dart';
import '../crud/crud_common.dart' show confirmDelete;
import 'face_api.dart';
import 'face_capture_page.dart';

/// Nhân viên tự quản lý khuôn mặt của mình: xem trạng thái, đăng ký / đăng ký lại (ghi đè),
/// thử nhận diện và xóa. Khuôn mặt chỉ dùng để đăng nhập nhanh.
class FaceEnrollPage extends StatefulWidget {
  final SessionStore session;
  const FaceEnrollPage({super.key, required this.session});

  @override
  State<FaceEnrollPage> createState() => _FaceEnrollPageState();
}

class _FaceEnrollPageState extends State<FaceEnrollPage> {
  late final FaceApi _api = FaceApi(widget.session.api);
  bool _busy = false;
  FaceVerifyResult? _test;

  Future<void> _run(Future<void> Function() job) async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      await job();
    } catch (e) {
      showError(e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _toast(String text) =>
      messengerKey.currentState?.showSnackBar(SnackBar(content: Text(text)));

  Future<void> _enroll() => _run(() async {
        final emb =
            await FaceCapturePage.open(context, title: 'Đăng ký khuôn mặt');
        if (emb == null) return;
        await _api.enroll(emb);
        await widget.session.restore();
        _test = null;
        _toast('Đã đăng ký khuôn mặt');
      });

  Future<void> _verify() => _run(() async {
        final emb =
            await FaceCapturePage.open(context, title: 'Thử nhận diện');
        if (emb == null) return;
        final r = await _api.verify(emb);
        if (mounted) setState(() => _test = r);
      });

  Future<void> _remove() async {
    if (!await confirmDelete(context, 'khuôn mặt đã đăng ký')) return;
    await _run(() async {
      await _api.remove();
      await widget.session.restore();
      _test = null;
      _toast('Đã xóa khuôn mặt');
    });
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.session,
      builder: (context, _) {
        final has = widget.session.user?.hasFace ?? false;
        final scheme = Theme.of(context).colorScheme;
        final color = has ? const Color(0xFF16A34A) : scheme.outline;
        return Scaffold(
          appBar: AppBar(title: const Text('Khuôn mặt của tôi')),
          body: ListView(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
            children: [
              FadeSlideIn(
                child: Card(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      children: [
                        AnimatedContainer(
                          duration: const Duration(milliseconds: 400),
                          width: 96,
                          height: 96,
                          decoration: BoxDecoration(
                            shape: BoxShape.circle,
                            color: color.withValues(alpha: 0.14),
                          ),
                          child: AnimatedSwitcher(
                            duration: const Duration(milliseconds: 300),
                            transitionBuilder: (c, a) =>
                                ScaleTransition(scale: a, child: c),
                            child: Icon(
                              has ? Icons.verified_user_rounded : Icons.face_retouching_off,
                              key: ValueKey(has),
                              size: 48,
                              color: color,
                            ),
                          ),
                        ),
                        const SizedBox(height: 16),
                        Text(
                          has ? 'Đã đăng ký khuôn mặt' : 'Chưa đăng ký khuôn mặt',
                          style: Theme.of(context)
                              .textTheme
                              .titleLarge
                              ?.copyWith(fontWeight: FontWeight.w800),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          has
                              ? 'Bạn có thể đăng nhập nhanh bằng khuôn mặt ở màn Đăng nhập.'
                              : 'Đăng ký để đăng nhập nhanh bằng khuôn mặt, không cần mã OTP.',
                          textAlign: TextAlign.center,
                          style: TextStyle(color: scheme.onSurfaceVariant),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 12),
              FadeSlideIn(
                index: 1,
                child: Text(
                  'Nhìn thẳng vào camera, chớp mắt một lần. Đăng ký lại sẽ ghi đè khuôn mặt cũ.',
                  style: TextStyle(color: scheme.onSurfaceVariant),
                ),
              ),
              const SizedBox(height: 16),
              FadeSlideIn(
                index: 2,
                child: FilledButton.icon(
                  onPressed: _busy ? null : _enroll,
                  icon: const Icon(Icons.face),
                  label: Text(has ? 'Đăng ký lại' : 'Đăng ký khuôn mặt'),
                ),
              ),
              if (has) ...[
                const SizedBox(height: 12),
                FadeSlideIn(
                  index: 3,
                  child: OutlinedButton.icon(
                    onPressed: _busy ? null : _verify,
                    icon: const Icon(Icons.center_focus_strong_rounded),
                    label: const Text('Thử nhận diện'),
                  ),
                ),
                const SizedBox(height: 12),
                FadeSlideIn(
                  index: 4,
                  child: OutlinedButton.icon(
                    onPressed: _busy ? null : _remove,
                    style: OutlinedButton.styleFrom(
                      foregroundColor: scheme.error,
                      side: BorderSide(color: scheme.error.withValues(alpha: 0.5)),
                    ),
                    icon: const Icon(Icons.delete_outline),
                    label: const Text('Xóa khuôn mặt'),
                  ),
                ),
              ],
              AnimatedSize(
                duration: const Duration(milliseconds: 300),
                curve: Curves.easeOutCubic,
                alignment: Alignment.topCenter,
                child: _test == null
                    ? const SizedBox(width: double.infinity)
                    : Padding(
                        padding: const EdgeInsets.only(top: 16),
                        child: _TestResult(result: _test!),
                      ),
              ),
            ],
          ),
        );
      },
    );
  }
}

/// Kết quả thử nhận diện: khớp / không khớp kèm độ giống (cosine).
class _TestResult extends StatelessWidget {
  final FaceVerifyResult result;
  const _TestResult({required this.result});

  @override
  Widget build(BuildContext context) {
    final ok = result.matched;
    final color = ok ? const Color(0xFF16A34A) : const Color(0xFFDC2626);
    return Card(
      key: const Key('face-test-result'),
      child: ListTile(
        leading: IconBadge(
            icon: ok ? Icons.check_rounded : Icons.close_rounded, color: color),
        title: Text(ok ? 'Khớp khuôn mặt đã đăng ký' : 'Không khớp',
            style: TextStyle(color: color, fontWeight: FontWeight.w700)),
        subtitle: Text('Độ giống ${(result.score * 100).toStringAsFixed(1)}%'),
      ),
    );
  }
}
