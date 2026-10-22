import 'dart:math' as math;

import 'package:device_info_plus/device_info_plus.dart';
import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:latlong2/latlong.dart';

import 'location_picker_page.dart';

class GeoFix {
  final double lat;
  final double lng;
  final bool isMocked;
  const GeoFix(this.lat, this.lng, {required this.isMocked});
}

/// Khoảng cách (m) giữa hai điểm, công thức haversine.
double distanceMeters(double lat1, double lng1, double lat2, double lng2) {
  const r = 6371000.0;
  double rad(double d) => d * math.pi / 180;
  final a = math.pow(math.sin(rad(lat2 - lat1) / 2), 2) +
      math.cos(rad(lat1)) *
          math.cos(rad(lat2)) *
          math.pow(math.sin(rad(lng2 - lng1) / 2), 2);
  return 2 * r * math.asin(math.sqrt(a.toDouble()));
}

/// Máy thật: GPS thật. Emulator: chọn điểm trên bản đồ (báo đúng điểm đã chọn, isMocked = true).
class GeoService {
  static bool? _physical;
  static LatLng? _lastPicked;

  static Future<bool> isPhysicalDevice() async {
    if (_physical != null) return _physical!;
    try {
      final info = await DeviceInfoPlugin().androidInfo;
      _physical = info.isPhysicalDevice;
    } catch (_) {
      _physical = true;
    }
    return _physical!;
  }

  /// [defaultPoint]: điểm mở sẵn trên bản đồ của emulator (điểm chấm công đầu tiên).
  /// Trả null nếu người dùng hủy. Ném [Exception] với thông báo tiếng Việt nếu thiếu quyền/GPS tắt.
  static Future<GeoFix?> current(BuildContext context,
      {LatLng? defaultPoint}) async {
    if (!await isPhysicalDevice()) {
      if (!context.mounted) return null;
      final p = await Navigator.of(context).push<LatLng>(MaterialPageRoute(
        builder: (_) => LocationPickerPage(
            initial:
                _lastPicked ?? defaultPoint ?? const LatLng(10.7769, 106.7009)),
      ));
      if (p == null) return null;
      _lastPicked = p;
      return GeoFix(p.latitude, p.longitude, isMocked: true);
    }
    if (!await Geolocator.isLocationServiceEnabled()) {
      throw Exception('GPS đang tắt, hãy bật vị trí trên điện thoại');
    }
    var perm = await Geolocator.checkPermission();
    if (perm == LocationPermission.denied) {
      perm = await Geolocator.requestPermission();
    }
    if (perm == LocationPermission.denied ||
        perm == LocationPermission.deniedForever) {
      throw Exception('Chưa được cấp quyền vị trí');
    }
    final pos = await Geolocator.getCurrentPosition(
        locationSettings:
            const LocationSettings(accuracy: LocationAccuracy.high));
    return GeoFix(pos.latitude, pos.longitude, isMocked: pos.isMocked);
  }
}
