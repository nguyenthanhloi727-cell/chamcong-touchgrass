import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../../core/widgets/map_tiles.dart';

/// Emulator không có GPS thật: chạm bản đồ (dữ liệu OpenStreetMap) để chọn vị trí.
class LocationPickerPage extends StatefulWidget {
  final LatLng initial;
  const LocationPickerPage({super.key, required this.initial});

  @override
  State<LocationPickerPage> createState() => _LocationPickerPageState();
}

class _LocationPickerPageState extends State<LocationPickerPage> {
  late LatLng _point = widget.initial;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Chọn vị trí (emulator)')),
      body: FlutterMap(
        options: MapOptions(
          initialCenter: widget.initial,
          initialZoom: 16,
          onTap: (_, p) => setState(() => _point = p),
        ),
        children: [
          appTileLayer(),
          MarkerLayer(markers: [
            Marker(
              point: _point,
              width: 40,
              height: 40,
              child:
                  const Icon(Icons.location_pin, color: Colors.red, size: 40),
            ),
          ]),
        ],
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(children: [
            Expanded(
                child: Text(
                    '${_point.latitude.toStringAsFixed(6)}, ${_point.longitude.toStringAsFixed(6)}')),
            // Theme đặt minimumSize rộng vô hạn; trong Row phải ghi đè, nếu không layout vỡ và màn trắng.
            FilledButton(
                style: FilledButton.styleFrom(minimumSize: const Size(0, 48)),
                onPressed: () => Navigator.pop(context, _point),
                child: const Text('Dùng vị trí này')),
          ]),
        ),
      ),
    );
  }
}
