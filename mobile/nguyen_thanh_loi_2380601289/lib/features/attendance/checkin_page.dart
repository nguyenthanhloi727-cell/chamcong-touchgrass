import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../core/api/api_client.dart';
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
      body: MobileScanner(
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
    );
  }
}

/// Luồng chấm công của Nhân viên: vị trí → quét QR → (khuôn mặt, Bước 15) → gửi check-in.
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
      if (qr == null) return;
      // Bước 15 sẽ lấy faceToken ở đây; chưa có thì gửi không kèm → faceMatched = false.
      setState(() => _step = '3/3 Gửi chấm công...');
      final r = await _api.checkIn(
          lat: fix.lat, lng: fix.lng, isMocked: fix.isMocked, qrToken: qr);
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Chấm công')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          SizedBox(
            height: 72,
            child: FilledButton.icon(
              onPressed: _busy ? null : _checkIn,
              icon: const Icon(Icons.login),
              label: const Text('Check-in', style: TextStyle(fontSize: 20)),
            ),
          ),
          const SizedBox(height: 12),
          SizedBox(
            height: 56,
            child: OutlinedButton.icon(
              onPressed: _busy ? null : _checkOut,
              icon: const Icon(Icons.logout),
              label: const Text('Check-out'),
            ),
          ),
          const SizedBox(height: 12),
          SizedBox(
            height: 56,
            child: OutlinedButton.icon(
              onPressed: _busy ? null : _share,
              icon: const Icon(Icons.share_location),
              label: const Text('Chia sẻ vị trí hiện tại'),
            ),
          ),
          if (_busy && _step.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 16),
              child: Center(child: Text(_step)),
            ),
          if (_result != null) ...[
            const SizedBox(height: 16),
            TrustResultCard(result: _result!),
          ],
        ],
      ),
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

/// Màn kết quả (Đề mục 6): điểm tin cậy to + 4 dòng chi tiết + trạng thái.
class TrustResultCard extends StatelessWidget {
  final CheckInResult result;
  const TrustResultCard({super.key, required this.result});

  @override
  Widget build(BuildContext context) {
    final r = result;
    final text = Theme.of(context).textTheme;
    Widget row(String label, int got, int max) => ListTile(
          dense: true,
          leading: Icon(got > 0 ? Icons.check_circle : Icons.cancel,
              color: got > 0 ? Colors.green : Colors.red),
          title: Text(label),
          trailing: Text('$got/$max'),
        );
    return Card(
      key: const Key('trust-result'),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Text('Điểm tin cậy', style: text.titleMedium),
            Text('${r.trustScore}/100',
                key: const Key('trust-score'),
                style: text.displayMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: r.trustScore >= 70 ? Colors.green : Colors.orange)),
            row('Khuôn mặt', r.face, 35),
            row('GPS (cách ${r.distance.toStringAsFixed(0)} m)', r.gps, 30),
            row('Mã QR', r.qr, 25),
            row('Không dùng GPS giả', r.notMocked, 10),
            const Divider(),
            Text('Trạng thái: ${statusText(r.status)}',
                key: const Key('trust-status'), style: text.titleMedium),
          ],
        ),
      ),
    );
  }
}
