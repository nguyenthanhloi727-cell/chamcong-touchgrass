import 'dart:async';

import 'package:flutter/material.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../../core/api/api_client.dart';
import 'attendance_api.dart';
import 'geo_service.dart';

/// Màn QR cho Admin/Quản lý: lấy mã mỗi 30s, hiện QR to + đếm ngược.
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
    return Scaffold(
      appBar: AppBar(title: const Text('Màn QR chấm công')),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (q != null)
                QrImageView(
                    data: q.token,
                    size: 280,
                    backgroundColor: Colors.white,
                    key: const Key('qr'))
              else if (_error != null)
                Text('Không tải được QR: ${apiErrorMessage(_error!)}')
              else
                const CircularProgressIndicator(),
              const SizedBox(height: 16),
              if (q != null)
                Text('Đổi mã sau $_left giây',
                    style: Theme.of(context).textTheme.titleLarge),
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
