import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../../core/api/api_client.dart';
import '../../core/widgets/map_tiles.dart';
import 'attendance_api.dart';
import 'review_page.dart';

/// Tọa độ chia sẻ mới nhất của nhân viên: danh sách + bản đồ (OpenStreetMap).
class TeamLocationPage extends StatefulWidget {
  final ApiClient api;
  const TeamLocationPage({super.key, required this.api});

  @override
  State<TeamLocationPage> createState() => _TeamLocationPageState();
}

class _TeamLocationPageState extends State<TeamLocationPage> {
  late final AttendanceApi _api = AttendanceApi(widget.api);
  late Future<List<TeamLocation>> _data = _api.team();

  String _t(DateTime d) =>
      '${d.hour.toString().padLeft(2, '0')}:${d.minute.toString().padLeft(2, '0')} '
      '${d.day}/${d.month}';

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Vị trí nhân viên')),
      body: FutureBuilder<List<TeamLocation>>(
        future: _data,
        builder: (context, snap) {
          if (snap.connectionState != ConnectionState.done) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snap.hasError) {
            return Center(child: Text(apiErrorMessage(snap.error!)));
          }
          final list = snap.data!;
          if (list.isEmpty) {
            return const Center(child: Text('Chưa có ai chia sẻ vị trí'));
          }
          return Column(children: [
            SizedBox(
              height: 260,
              child: FlutterMap(
                options: MapOptions(
                    initialCenter: LatLng(list.first.lat, list.first.lng),
                    initialZoom: 14),
                children: [
                  appTileLayer(),
                  MarkerLayer(markers: [
                    for (final l in list)
                      Marker(
                        point: LatLng(l.lat, l.lng),
                        width: 36,
                        height: 36,
                        child: const Icon(Icons.person_pin_circle,
                            color: Colors.blue, size: 36),
                      ),
                  ]),
                ],
              ),
            ),
            Expanded(
              child: RefreshIndicator(
                onRefresh: () async {
                  final f = _api.team();
                  setState(() {
                    _data = f;
                  });
                  await f;
                },
                child: ListView(children: [
                  for (final l in list)
                    ListTile(
                      leading: const Icon(Icons.person_pin_circle),
                      title: Text(l.fullName),
                      subtitle: Text(
                          '${l.lat.toStringAsFixed(6)}, ${l.lng.toStringAsFixed(6)} · ${_t(l.updatedAt)}'),
                      trailing: IconButton(
                          icon: const Icon(Icons.map),
                          onPressed: () => openMaps(l.lat, l.lng)),
                    ),
                ]),
              ),
            ),
          ]);
        },
      ),
    );
  }
}
