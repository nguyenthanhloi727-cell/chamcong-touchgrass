import 'dart:async';

import 'package:flutter/material.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../../core/api/api_client.dart';
import '../../core/widgets/animations.dart';
import '../../core/widgets/ui.dart';
import 'attendance_api.dart';
import 'geo_service.dart';

/// Màn QR cho Admin/Quản lý: lấy mã mới khi hết hạn (theo expiresIn của server), hiện QR to + đếm ngược.
/// Admin có thêm nút đặt vị trí hiện tại làm điểm chấm công (bán kính 200 m).
class QrPage extends StatefulWidget {
  final ApiClient api;
  final bool isAdmin;
  const QrPage({super.key, required this.api, required this.isAdmin});

  @override
  State<QrPage> createState() => _QrPageState();
}

class _QrPageState extends State<QrPage> {
  late final AttendanceApi _api = AttendanceApi(widget.api);
  Timer? _tick;
  QrInfo? _qr;
  int _left = 0;
  int _total = 0; // độ dài cửa sổ QR lớn nhất đã thấy, làm mốc cho vòng đếm ngược
  Object? _error;
  bool _loading = false;

  @override
  void initState() {
    super.initState();
    _fetch();
    _tick = Timer.periodic(const Duration(seconds: 1), (_) {
      if (!mounted) return;
      if (_left > 0) setState(() => _left--);
      if (_left <= 0 && !_loading) _fetch();
    });
  }

  @override
  void dispose() {
    _tick?.cancel();
    super.dispose();
  }

  Future<void> _fetch() async {
    _loading = true;
    try {
      final q = await _api.qr();
      if (!mounted) return;
      setState(() {
        _qr = q;
        _left = q.expiresIn;
        if (q.expiresIn > _total) _total = q.expiresIn;
        _error = null;
      });
    } catch (e) {
      if (mounted) setState(() => _error = e);
    } finally {
      _loading = false;
    }
  }

  Future<void> _setBranchPoint() async {
    try {
      final existing = await _api.locations();
      if (!mounted) return;
      final fix = await GeoService.current(context,
          defaultPoint: existing.isEmpty ? null : existing.first.point);
      if (fix == null) return;
      final name = 'Điểm chấm công ${existing.length + 1}';
      await _api.createLocation(name, fix.lat, fix.lng, radius: 200);
      messengerKey.currentState?.showSnackBar(SnackBar(
          content: Text(
              'Đã thêm "$name" (${fix.lat.toStringAsFixed(5)}, ${fix.lng.toStringAsFixed(5)}), bán kính 200 m')));
    } catch (e) {
      showError(e);
    }
  }

  @override
  Widget build(BuildContext context) {
    final q = _qr;
    final scheme = Theme.of(context).colorScheme;
    // Vòng đếm ngược: xanh → cam → đỏ khi sắp hết hạn.
    final fraction = _total <= 0 ? 0.0 : (_left / _total).clamp(0.0, 1.0);
    final ringColor = _left <= 5
        ? const Color(0xFFDC2626)
        : _left <= 15
            ? const Color(0xFFEA580C)
            : scheme.primary;
    return Scaffold(
      appBar: AppBar(title: const Text('Màn QR chấm công')),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (q != null)
                SizedBox(
                  width: 340,
                  height: 340,
                  child: Stack(
                    alignment: Alignment.center,
                    children: [
                      TweenAnimationBuilder<double>(
                        tween: Tween(end: fraction),
                        duration: const Duration(milliseconds: 1000),
                        curve: Curves.linear,
                        builder: (context, v, _) => SizedBox(
                          width: 340,
                          height: 340,
                          child: CircularProgressIndicator(
                            value: v,
                            strokeWidth: 8,
                            strokeCap: StrokeCap.round,
                            color: ringColor,
                            backgroundColor:
                                ringColor.withValues(alpha: 0.15),
                          ),
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(24),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withValues(alpha: 0.08),
                              blurRadius: 20,
                              offset: const Offset(0, 8),
                            ),
                          ],
                        ),
                        child: AnimatedSwitcher(
                          duration: const Duration(milliseconds: 450),
                          transitionBuilder: (child, anim) => FadeTransition(
                            opacity: anim,
                            child: ScaleTransition(
                              scale: Tween(begin: 0.92, end: 1.0).animate(anim),
                              child: child,
                            ),
                          ),
                          child: QrImageView(
                            key: ValueKey(q.token),
                            data: q.token,
                            size: 216,
                            backgroundColor: Colors.white,
                          ),
                        ),
                      ),
                    ],
                  ),
                )
              else if (_error != null)
                EmptyState(
                    icon: Icons.qr_code_2_rounded,
                    text: 'Không tải được QR: ${apiErrorMessage(_error!)}')
              else
                const SizedBox(
                    width: 340, height: 340, child: SkeletonBox(radius: 28)),
              const SizedBox(height: 20),
              if (q != null)
                AnimatedDefaultTextStyle(
                  duration: const Duration(milliseconds: 300),
                  style: Theme.of(context).textTheme.titleLarge!.copyWith(
                        color: ringColor,
                        fontWeight: FontWeight.w700,
                      ),
                  child: Text('Đổi mã sau $_left giây'),
                ),
              const SizedBox(height: 24),
              if (widget.isAdmin)
                OutlinedButton.icon(
                  onPressed: _setBranchPoint,
                  icon: const Icon(Icons.add_location_alt),
                  label: const Text('Đặt vị trí hiện tại làm điểm chấm công'),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
