import 'package:flutter/material.dart';

import '../../core/api/api_client.dart';
import '../dashboard/dashboard_models.dart' show statusLabel;
import 'crud_common.dart';

const _statuses = ['OnTime', 'Late', 'PendingReview', 'Rejected'];

String _day(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

String _dmy(DateTime d) =>
    '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

/// Chấm công: lọc theo ngày (hoặc tất cả), sửa trạng thái, thêm tay, xóa.
class AttendancesPage extends StatefulWidget {
  final ApiClient api;
  const AttendancesPage({super.key, required this.api});

  @override
  State<AttendancesPage> createState() => _AttendancesPageState();
}

class _AttendancesPageState extends State<AttendancesPage> {
  DateTime? _date = DateTime.now();

  Future<void> _pickDate() async {
    final d = await showDatePicker(
      context: context,
      initialDate: _date ?? DateTime.now(),
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (d != null) setState(() => _date = d);
  }

  @override
  Widget build(BuildContext context) {
    final api = widget.api;
    return CrudListPage(
      key: ValueKey(_date),
      title: 'Chấm công',
      header: Padding(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
        child: Row(
          children: [
            ActionChip(
              avatar: const Icon(Icons.event, size: 18),
              label: Text(_date == null ? 'Tất cả các ngày' : _dmy(_date!)),
              onPressed: _pickDate,
            ),
            const SizedBox(width: 8),
            if (_date != null)
              TextButton(
                  onPressed: () => setState(() => _date = null),
                  child: const Text('Xem tất cả')),
          ],
        ),
      ),
      load: (q) => fetchList(api, '/api/attendances', query: {
        if (q.isNotEmpty) 'q': q,
        if (_date != null) 'from': _day(_date!),
        if (_date != null) 'to': _day(_date!),
      }),
      titleOf: (a) => '${a['fullName']} (${a['username']})',
      subtitleOf: (a) {
        final inAt = DateTime.parse(a['checkInAt'] as String);
        final out = a['checkOutAt'] == null
            ? '—'
            : _hm(DateTime.parse(a['checkOutAt'] as String));
        return '${_dmy(inAt)} ${_hm(inAt)} → $out · ${statusLabel(a['status'] as String)} · ${a['trustScore']}đ';
      },
      form: (context, item) => showDialog<bool>(
        context: context,
        builder: (_) => _AttendanceForm(api: api, item: item),
      ),
      onDelete: (a) => api.dio.delete('/api/attendances/${a['id']}'),
    );
  }
}

String _hm(DateTime d) =>
    '${d.hour.toString().padLeft(2, '0')}:${d.minute.toString().padLeft(2, '0')}';

class _AttendanceForm extends StatefulWidget {
  final ApiClient api;
  final Json? item;
  const _AttendanceForm({required this.api, this.item});

  @override
  State<_AttendanceForm> createState() => _AttendanceFormState();
}

class _AttendanceFormState extends State<_AttendanceForm> {
  final _key = GlobalKey<FormState>();
  late String _status = (widget.item?['status'] as String?) ?? 'OnTime';
  int? _userId;
  int? _shiftId;
  late DateTime _checkIn = widget.item == null
      ? DateTime.now()
      : DateTime.parse(widget.item!['checkInAt'] as String);
  List<Json> _users = [];
  List<Json> _shifts = [];

  bool get _isEdit => widget.item != null;

  @override
  void initState() {
    super.initState();
    _shiftId = widget.item?['shiftId'] as int?;
    Future.wait([
      fetchList(widget.api, '/api/employees'),
      fetchList(widget.api, '/api/shifts'),
    ]).then((r) {
      if (!mounted) return;
      setState(() {
        _users = r[0];
        _shifts = r[1];
      });
    }).catchError((Object e) {
      showError(e);
    });
  }

  Future<void> _pickCheckIn() async {
    final d = await showDatePicker(
      context: context,
      initialDate: _checkIn,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (d == null || !mounted) return;
    final t = await showTimePicker(
        context: context, initialTime: TimeOfDay.fromDateTime(_checkIn));
    if (t == null) return;
    setState(() => _checkIn = DateTime(d.year, d.month, d.day, t.hour, t.minute));
  }

  Future<void> _submit() async {
    final dio = widget.api.dio;
    if (_isEdit) {
      await dio.put('/api/attendances/${widget.item!['id']}', data: {
        'status': _status,
        'shiftId': _shiftId,
      });
    } else {
      await dio.post('/api/attendances', data: {
        'userId': _userId,
        'shiftId': _shiftId,
        'checkInAt': _checkIn.toIso8601String(),
        'status': _status,
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final shiftIds = _shifts.map((s) => s['id']).toSet();
    return FormDialog(
      title: _isEdit ? 'Sửa trạng thái chấm công' : 'Thêm chấm công',
      formKey: _key,
      onSubmit: _submit,
      children: [
        if (_isEdit)
          Text('${widget.item!['fullName']} · ${_dmy(_checkIn)} ${_hm(_checkIn)}')
        else ...[
          DropdownButtonFormField<int>(
            key: ValueKey('user-${_users.length}'),
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Nhân viên'),
            items: [
              for (final u in _users)
                DropdownMenuItem(
                    value: u['id'] as int,
                    child: Text('${u['fullName']} (${u['username']})')),
            ],
            validator: (v) => v == null ? 'Chọn nhân viên' : null,
            onChanged: (v) => _userId = v,
          ),
          OutlinedButton.icon(
            icon: const Icon(Icons.event),
            label: Text('Giờ vào: ${_dmy(_checkIn)} ${_hm(_checkIn)}'),
            onPressed: _pickCheckIn,
          ),
        ],
        DropdownButtonFormField<int?>(
          key: ValueKey('shift-${_shifts.length}'),
          initialValue: shiftIds.contains(_shiftId) ? _shiftId : null,
          isExpanded: true,
          decoration: const InputDecoration(labelText: 'Ca làm'),
          items: [
            const DropdownMenuItem(value: null, child: Text('— Không có —')),
            for (final s in _shifts)
              DropdownMenuItem(
                  value: s['id'] as int, child: Text(s['name'] as String)),
          ],
          onChanged: (v) => _shiftId = v,
        ),
        DropdownButtonFormField<String>(
          initialValue: _status,
          decoration: const InputDecoration(labelText: 'Trạng thái'),
          items: [
            for (final s in _statuses)
              DropdownMenuItem(value: s, child: Text(statusLabel(s))),
          ],
          onChanged: (v) => _status = v ?? _status,
        ),
      ],
    );
  }
}
