import 'package:flutter/material.dart';

import '../../core/api/api_client.dart';
import 'crud_common.dart';

const roleGroupLabels = {'Manager': 'Quản lý', 'Employee': 'Nhân viên'};

// ---------------------------------------------------------------- Phòng ban

class DepartmentsPage extends StatelessWidget {
  final ApiClient api;
  const DepartmentsPage({super.key, required this.api});

  @override
  Widget build(BuildContext context) {
    return CrudListPage(
      title: 'Phòng ban',
      load: (q) => fetchList(api, '/api/departments',
          query: {if (q.isNotEmpty) 'q': q}),
      titleOf: (d) => d['name'] as String,
      subtitleOf: (d) =>
          'Trưởng phòng: ${d['managerName'] ?? '—'} · ${d['employeeCount']} nhân viên',
      form: (context, item) => showDialog<bool>(
        context: context,
        builder: (_) => _DepartmentForm(api: api, item: item),
      ),
      onDelete: (d) => api.dio.delete('/api/departments/${d['id']}'),
    );
  }
}

class _DepartmentForm extends StatefulWidget {
  final ApiClient api;
  final Json? item;
  const _DepartmentForm({required this.api, this.item});

  @override
  State<_DepartmentForm> createState() => _DepartmentFormState();
}

class _DepartmentFormState extends State<_DepartmentForm> {
  final _key = GlobalKey<FormState>();
  late final _name = TextEditingController(text: widget.item?['name'] as String?);
  int? _managerId;
  List<Json> _managers = [];

  @override
  void initState() {
    super.initState();
    _managerId = widget.item?['managerId'] as int?;
    fetchList(widget.api, '/api/employees').then((all) {
      if (!mounted) return;
      setState(() {
        _managers =
            all.where((e) => e['role'] == 'Manager' || e['role'] == 'Admin').toList();
      });
    }).catchError((Object e) {
      showError(e);
    });
  }

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final body = {'name': _name.text.trim(), 'managerId': _managerId};
    if (widget.item == null) {
      await widget.api.dio.post('/api/departments', data: body);
    } else {
      await widget.api.dio.put('/api/departments/${widget.item!['id']}', data: body);
    }
  }

  @override
  Widget build(BuildContext context) {
    final ids = _managers.map((m) => m['id']).toSet();
    return FormDialog(
      title: widget.item == null ? 'Thêm phòng ban' : 'Sửa phòng ban',
      formKey: _key,
      onSubmit: _submit,
      children: [
        TextFormField(
          controller: _name,
          decoration: const InputDecoration(labelText: 'Tên phòng ban'),
          validator: notEmpty,
        ),
        DropdownButtonFormField<int?>(
          key: ValueKey('mgr-${_managers.length}'),
          initialValue: ids.contains(_managerId) ? _managerId : null,
          isExpanded: true,
          decoration: const InputDecoration(labelText: 'Trưởng phòng'),
          items: [
            const DropdownMenuItem(value: null, child: Text('— Không có —')),
            for (final m in _managers)
              DropdownMenuItem(
                  value: m['id'] as int, child: Text(m['fullName'] as String)),
          ],
          onChanged: (v) => _managerId = v,
        ),
      ],
    );
  }
}

// ------------------------------------------------------------------ Chức vụ

class PositionsPage extends StatelessWidget {
  final ApiClient api;
  const PositionsPage({super.key, required this.api});

  @override
  Widget build(BuildContext context) {
    return CrudListPage(
      title: 'Chức vụ',
      load: (q) =>
          fetchList(api, '/api/positions', query: {if (q.isNotEmpty) 'q': q}),
      titleOf: (p) => p['name'] as String,
      subtitleOf: (p) => roleGroupLabels[p['roleGroup']] ?? '${p['roleGroup']}',
      form: (context, item) => showDialog<bool>(
        context: context,
        builder: (_) => _PositionForm(api: api, item: item),
      ),
      onDelete: (p) => api.dio.delete('/api/positions/${p['id']}'),
    );
  }
}

class _PositionForm extends StatefulWidget {
  final ApiClient api;
  final Json? item;
  const _PositionForm({required this.api, this.item});

  @override
  State<_PositionForm> createState() => _PositionFormState();
}

class _PositionFormState extends State<_PositionForm> {
  final _key = GlobalKey<FormState>();
  late final _name = TextEditingController(text: widget.item?['name'] as String?);
  late String _group = (widget.item?['roleGroup'] as String?) ?? 'Employee';

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final body = {'name': _name.text.trim(), 'roleGroup': _group};
    if (widget.item == null) {
      await widget.api.dio.post('/api/positions', data: body);
    } else {
      await widget.api.dio.put('/api/positions/${widget.item!['id']}', data: body);
    }
  }

  @override
  Widget build(BuildContext context) {
    return FormDialog(
      title: widget.item == null ? 'Thêm chức vụ' : 'Sửa chức vụ',
      formKey: _key,
      onSubmit: _submit,
      children: [
        TextFormField(
          controller: _name,
          decoration: const InputDecoration(labelText: 'Tên chức vụ'),
          validator: notEmpty,
        ),
        DropdownButtonFormField<String>(
          initialValue: _group,
          decoration: const InputDecoration(labelText: 'Nhóm (Role)'),
          items: [
            for (final e in roleGroupLabels.entries)
              DropdownMenuItem(value: e.key, child: Text(e.value)),
          ],
          onChanged: (v) => _group = v ?? _group,
        ),
      ],
    );
  }
}

// ------------------------------------------------------------------- Ca làm

class ShiftsPage extends StatelessWidget {
  final ApiClient api;
  const ShiftsPage({super.key, required this.api});

  @override
  Widget build(BuildContext context) {
    return CrudListPage(
      title: 'Ca làm',
      load: (q) =>
          fetchList(api, '/api/shifts', query: {if (q.isNotEmpty) 'q': q}),
      titleOf: (s) => s['name'] as String,
      subtitleOf: (s) =>
          '${(s['startTime'] as String).substring(0, 5)} - ${(s['endTime'] as String).substring(0, 5)}',
      form: (context, item) => showDialog<bool>(
        context: context,
        builder: (_) => _ShiftForm(api: api, item: item),
      ),
      onDelete: (s) => api.dio.delete('/api/shifts/${s['id']}'),
    );
  }
}

class _ShiftForm extends StatefulWidget {
  final ApiClient api;
  final Json? item;
  const _ShiftForm({required this.api, this.item});

  @override
  State<_ShiftForm> createState() => _ShiftFormState();
}

class _ShiftFormState extends State<_ShiftForm> {
  final _key = GlobalKey<FormState>();
  late final _name = TextEditingController(text: widget.item?['name'] as String?);
  late TimeOfDay _start = widget.item == null
      ? const TimeOfDay(hour: 8, minute: 0)
      : parseTime(widget.item!['startTime'] as String);
  late TimeOfDay _end = widget.item == null
      ? const TimeOfDay(hour: 17, minute: 0)
      : parseTime(widget.item!['endTime'] as String);

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  Future<void> _pick(bool isStart) async {
    final t = await showTimePicker(
        context: context, initialTime: isStart ? _start : _end);
    if (t != null) setState(() => isStart ? _start = t : _end = t);
  }

  Future<void> _submit() async {
    final body = {
      'name': _name.text.trim(),
      'startTime': '${hhmm(_start)}:00',
      'endTime': '${hhmm(_end)}:00',
    };
    if (widget.item == null) {
      await widget.api.dio.post('/api/shifts', data: body);
    } else {
      await widget.api.dio.put('/api/shifts/${widget.item!['id']}', data: body);
    }
  }

  @override
  Widget build(BuildContext context) {
    return FormDialog(
      title: widget.item == null ? 'Thêm ca làm' : 'Sửa ca làm',
      formKey: _key,
      onSubmit: _submit,
      children: [
        TextFormField(
          controller: _name,
          decoration: const InputDecoration(labelText: 'Tên ca'),
          validator: notEmpty,
        ),
        OutlinedButton.icon(
          icon: const Icon(Icons.access_time),
          label: Text('Giờ bắt đầu: ${hhmm(_start)}'),
          onPressed: () => _pick(true),
        ),
        OutlinedButton.icon(
          icon: const Icon(Icons.access_time_filled),
          label: Text('Giờ kết thúc: ${hhmm(_end)}'),
          onPressed: () => _pick(false),
        ),
      ],
    );
  }
}
