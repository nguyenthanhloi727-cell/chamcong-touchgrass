import 'package:flutter_map/flutter_map.dart';

/// Lớp ô bản đồ dùng chung (màn chọn vị trí, màn vị trí nhân viên).
///
/// Dùng bản sao OpenStreetMap của OSM Đức thay cho tile.openstreetmap.org: máy chủ chính ngắt
/// kết nối ("Connection reset by peer") với mạng này nên bản đồ chỉ hiện nền xám. CARTO thì
/// đóng dấu "API KEY REQUIRED" lên mọi ô, nên không dùng.
TileLayer appTileLayer() => TileLayer(
      urlTemplate: 'https://tile.openstreetmap.de/{z}/{x}/{y}.png',
      userAgentPackageName: 'com.touchgrass.nguyen_thanh_loi_2380601289',
    );
