import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../core/api/api_client.dart';
import '../../core/widgets/animations.dart';
import '../../core/widgets/ui.dart';
import 'attendance_api.dart';
import 'geo_service.dart';

/// Quét QR bằng camera sau; trả về chuỗi QR đầu tiên đọc được.
class QrScanPage extends StatefulWidget {
  const QrScanPage({super.key});

  @override
  State<QrScanPage> createState() => _QrScanPageState();
}

class _QrScanPageState extends State<QrScanPage> {
  bool _done = false;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Quét QR chấm công')),
      extendBodyBehindAppBar: true,
      body: Stack(
        fit: StackFit.expand,
        children: [
          MobileScanner(
            onDetect: (capture) {
              if (_done) return;
              for (final b in capture.barcodes) {
                final v = b.rawValue;
                if (v != null && v.isNotEmpty) {
                  _done = true;
                  Navigator.pop(context, v);
                  return;
                }
              }
            },
          ),
          const IgnorePointer(child: _ScanOverlay()),
        ],
      ),
    );
  }
}

/// Khung ngắm + vạch quét chạy lên xuống + dòng hướng dẫn.
class _ScanOverlay extends StatefulWidget {
  const _ScanOverlay();

  @override
  State<_ScanOverlay> createState() => _ScanOverlayState();
}

class _ScanOverlayState extends State<_ScanOverlay>
    with SingleTickerProviderStateMixin {
  late final AnimationController _c = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1800),
  )..repeat(reverse: true);

  @override
  void dispose() {
    _c.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    const size = 260.0;
    final primary = Theme.of(context).colorScheme.primary;
    return Stack(
      children: [
        Center(
          child: SizedBox(
            width: size,
            height: size,
            child: Stack(
              children: [
                Container(
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(28),
                    border: Border.all(color: Colors.white, width: 3),
                  ),
                ),
                AnimatedBuilder(
                  animation: _c,
                  builder: (context, _) => Positioned(
                    top: 12 + (size - 28) * Curves.easeInOut.transform(_c.value),
                    left: 14,
                    right: 14,
                    child: Container(
                      height: 3,
                      decoration: BoxDecoration(
                        color: primary,
                        borderRadius: BorderRadius.circular(2),
                        boxShadow: [
                          BoxShadow(
                              color: primary.withValues(alpha: 0.8),
                              blurRadius: 12),
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
        const Align(
          alignment: Alignment(0, 0.62),
          child: Text(
            'Đưa mã QR vào khung',
            style: TextStyle(
                color: Colors.white,
                fontSize: 16,
                fontWeight: FontWeight.w600,
                shadows: [Shadow(blurRadius: 8, color: Colors.black54)]),
          ),
        ),
      ],
    );
  }
}

/// Luồng chấm công của Nhân viên: vị trí → quét QR → gửi check-in.
class CheckinPage extends StatefulWidget {
  final ApiClient api;
  const CheckinPage({super.key, required this.api});

  @override
  State<CheckinPage> createState() => _CheckinPageState();
}

class _CheckinPageState extends State<CheckinPage> {
  late final AttendanceApi _api = AttendanceApi(widget.api);
  String _step = '';
  bool _busy = false;
  CheckInResult? _result;

  Future<GeoFix?> _locate() async {
    final branches = await _api.locations();
    if (!mounted) return null;
    return GeoService.current(context,
        defaultPoint: branches.isEmpty ? null : branches.first.point);
  }

  Future<void> _checkIn() async {
    setState(() {
      _busy = true;
      _result = null;
      _step = '1/3 Lấy vị trí...';
    });
    try {
      final fix = await _locate();
      if (fix == null || !mounted) return;
      setState(() => _step = '2/3 Quét QR...');
      final qr = await Navigator.of(context)
          .push<String>(MaterialPageRoute(builder: (_) => const QrScanPage()));
      if (qr == null || !mounted) return;
      setState(() => _step = '3/3 Gửi chấm công...');
      final r = await _api.checkIn(
          lat: fix.lat,
          lng: fix.lng,
          isMocked: fix.isMocked,
          qrToken: qr);
      if (mounted) setState(() => _result = r);
    } catch (e) {
      showError(e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _checkOut() async {
    setState(() => _busy = true);
    try {
      await _api.checkOut();
      messengerKey.currentState?.showSnackBar(
          const SnackBar(content: Text('Đã check-out')));
    } catch (e) {
      showError(e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _share() async {
    setState(() => _busy = true);
    try {
      final fix = await _locate();
      if (fix == null) return;
      await _api.shareLocation(fix.lat, fix.lng);
      messengerKey.currentState?.showSnackBar(
          const SnackBar(content: Text('Đã chia sẻ vị trí hiện tại')));
    } catch (e) {
      showError(e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  int get _stepIndex => _step.isEmpty ? 0 : int.tryParse(_step[0]) ?? 0;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Scaffold(
      appBar: AppBar(title: const Text('Chấm công')),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
        children: [
          FadeSlideIn(
            child: Card(
              margin: EdgeInsets.zero,
              child: Padding(
                padding: const EdgeInsets.all(18),
                child: _StepBar(current: _busy ? _stepIndex : 0),
              ),
            ),
          ),
          const SizedBox(height: 16),
          FadeSlideIn(
            index: 1,
            child: PressScale(
              child: SizedBox(
                height: 76,
                child: FilledButton.icon(
                  onPressed: _busy ? null : _checkIn,
                  icon: _busy
                      ? const SizedBox(
                          width: 22,
                          height: 22,
                          child: CircularProgressIndicator(
                              strokeWidth: 2.5, color: Colors.white))
                      : const Icon(Icons.login_rounded, size: 26),
                  label: Text(_busy && _step.isNotEmpty ? _step : 'Check-in',
                      style: const TextStyle(fontSize: 20)),
                ),
              ),
            ),
          ),
          const SizedBox(height: 12),
          FadeSlideIn(
            index: 2,
            child: Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: _busy ? null : _checkOut,
                    icon: const Icon(Icons.logout_rounded),
                    label: const Text('Check-out'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: _busy ? null : _share,
                    icon: const Icon(Icons.share_location_rounded),
                    label: const Text('Chia sẻ vị trí hiện tại',
                        textAlign: TextAlign.center),
                  ),
                ),
              ],
            ),
          ),
          AnimatedSize(
            duration: const Duration(milliseconds: 350),
            curve: Curves.easeOutCubic,
            alignment: Alignment.topCenter,
            child: _result == null
                ? const SizedBox(width: double.infinity)
                : Padding(
                    padding: const EdgeInsets.only(top: 16),
                    child: TrustResultCard(result: _result!),
                  ),
          ),
          if (_result == null && !_busy)
            Padding(
              padding: const EdgeInsets.only(top: 24),
              child: Text(
                'Bấm Check-in: app lấy vị trí, mở camera quét mã QR do quản lý hiển thị, rồi gửi chấm công.',
                textAlign: TextAlign.center,
                style: TextStyle(color: scheme.onSurfaceVariant),
              ),
            ),
        ],
      ),
    );
  }
}

/// 3 bước của luồng chấm công; bước hiện tại sáng lên, bước đã xong có dấu tích.
class _StepBar extends StatelessWidget {
  final int current; // 0 = chưa bắt đầu, 1..3
  const _StepBar({required this.current});

  @override
  Widget build(BuildContext context) {
    const labels = ['Vị trí', 'Quét QR', 'Gửi'];
    const icons = [
      Icons.my_location_rounded,
      Icons.qr_code_scanner_rounded,
      Icons.send_rounded,
    ];
    final scheme = Theme.of(context).colorScheme;
    return Row(
      children: [
        for (var i = 0; i < 3; i++) ...[
          if (i > 0)
            Expanded(
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 400),
                height: 3,
                decoration: BoxDecoration(
                  color: current > i ? scheme.primary : scheme.outlineVariant,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
          Column(
            children: [
              AnimatedContainer(
                duration: const Duration(milliseconds: 350),
                curve: Curves.easeOutBack,
                width: current == i + 1 ? 52 : 44,
                height: current == i + 1 ? 52 : 44,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: current > i + 1
                      ? scheme.primary
                      : current == i + 1
                          ? scheme.primaryContainer
                          : scheme.surfaceContainerHighest,
                  border: current == i + 1
                      ? Border.all(color: scheme.primary, width: 2)
                      : null,
                ),
                child: AnimatedSwitcher(
                  duration: const Duration(milliseconds: 250),
                  child: Icon(
                    current > i + 1 ? Icons.check_rounded : icons[i],
                    key: ValueKey(current > i + 1),
                    size: 22,
                    color: current > i + 1
                        ? scheme.onPrimary
                        : current == i + 1
                            ? scheme.primary
                            : scheme.onSurfaceVariant,
                  ),
                ),
              ),
              const SizedBox(height: 6),
              Text(labels[i],
                  style: TextStyle(
                      fontSize: 12,
                      fontWeight:
                          current == i + 1 ? FontWeight.w700 : FontWeight.w500,
                      color: current == i + 1
                          ? scheme.primary
                          : scheme.onSurfaceVariant)),
            ],
          ),
        ],
      ],
    );
  }
}

String statusText(String s) => switch (s) {
      'OnTime' => 'Đúng giờ',
      'Late' => 'Đi muộn',
      'PendingReview' => 'Chờ quản lý duyệt',
      'Rejected' => 'Bị từ chối',
      _ => s,
    };

/// Màn kết quả (Đề mục 6): vòng điểm tin cậy chạy từ 0 + 3 dòng chi tiết + trạng thái.
class TrustResultCard extends StatelessWidget {
  final CheckInResult result;
  const TrustResultCard({super.key, required this.result});

  @override
  Widget build(BuildContext context) {
    final r = result;
    final text = Theme.of(context).textTheme;
    final color = trustColor(r.trustScore);
    Widget row(int index, String label, int got, int max) => FadeSlideIn(
          index: index + 2,
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: 6),
            child: Row(
              children: [
                IconBadge(
                  icon: got > 0 ? Icons.check_rounded : Icons.close_rounded,
                  color: got > 0
                      ? const Color(0xFF16A34A)
                      : const Color(0xFFDC2626),
                  size: 34,
                ),
                const SizedBox(width: 12),
                Expanded(child: Text(label)),
                Text('$got/$max',
                    style: const TextStyle(fontWeight: FontWeight.w700)),
              ],
            ),
          ),
        );
    return Card(
      key: const Key('trust-result'),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          children: [
            Text('Điểm tin cậy', style: text.titleMedium),
            const SizedBox(height: 12),
            SizedBox(
              width: 150,
              height: 150,
              child: Stack(
                alignment: Alignment.center,
                children: [
                  TweenAnimationBuilder<double>(
                    tween: Tween(begin: 0, end: r.trustScore / 100),
                    duration: const Duration(milliseconds: 1200),
                    curve: Curves.easeOutCubic,
                    builder: (context, v, _) => SizedBox(
                      width: 150,
                      height: 150,
                      child: CircularProgressIndicator(
                        value: v,
                        strokeWidth: 12,
                        strokeCap: StrokeCap.round,
                        color: color,
                        backgroundColor: color.withValues(alpha: 0.15),
                      ),
                    ),
                  ),
                  CountUp(
                    value: r.trustScore,
                    suffix: '/100',
                    textKey: const Key('trust-score'),
                    style: text.headlineMedium
                        ?.copyWith(fontWeight: FontWeight.w800, color: color),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            row(0, 'GPS (cách ${r.distance.toStringAsFixed(0)} m)', r.gps, 45),
            row(1, 'Mã QR', r.qr, 35),
            row(2, 'Không dùng GPS giả', r.notMocked, 20),
            const Divider(height: 24),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text('Trạng thái: ', style: text.titleMedium),
                Text(statusText(r.status),
                    key: const Key('trust-status'),
                    style: text.titleMedium?.copyWith(
                        fontWeight: FontWeight.w800,
                        color: statusColor(r.status))),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
