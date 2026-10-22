import 'package:flutter/material.dart';

import '../../core/api/api_client.dart';
import 'crud_common.dart';

const roleLabels = {'Admin': 'Admin', 'Manager': 'Quản lý', 'Employee': 'Nhân viên'};

class EmployeesPage extends StatelessWidget {
  final ApiClient api;
  const EmployeesPage({super.key, required this.api});

  @override
  Widget build(BuildContext context) {
    return CrudListPage(
      title: 'Nhân viên',
      load: (q) =>
          fetchList(api, '/api/employees', query: {if (q.isNotEmpty) 'q': q}),
      titleOf: (e) => '${e['fullName']} (${e['username']})',
      subtitleOf: (e) {
        final pos = e['position'] as String?;
        final locked = e['status'] == 'Locked' ? ' · Đã khóa' : '';
        return '${roleLabels[e['role']] ?? e['role']}${pos == null ? '' : ' · $pos'}$locked';
      },
      form: (context, item) => showDialog<bool>(
        context: context,
        builder: (_) => EmployeeForm(api: api, item: item),
      ),
      onDelete: (e) => api.dio.delete('/api/employees/${e['id']}'),
    );
  }
}

/// Form nhân viên: ô 1 Role, ô 2 Chức vụ lọc theo ô 1 (đổi ô 1 → reset ô 2).
class EmployeeForm extends StatefulWidget {
  final ApiClient api;
  final Json? item;
  const EmployeeForm({super.key, required this.api, this.item});

  @override
  State<EmployeeForm> createState() => _EmployeeFormState();
}

class _EmployeeFormState extends State<EmployeeForm> {
  final _key = GlobalKey<FormState>();
  late final _username = TextEditingController(text: widget.item?['username'] as String?);
  final _password = TextEditingController();
  late final _fullName = TextEditingController(text: widget.item?['fullName'] as String?);
  late final _email = TextEditingController(text: widget.item?['email'] as String?);
  late final _phone = TextEditingController(text: widget.item?['phoneNumber'] as String?);

  late String _role = (widget.item?['role'] as String?) ?? 'Employee';
  int? _positionId;
  int? _departmentId;
  late String _status = (widget.item?['status'] as String?) == 'Locked' ? 'Locked' : 'Active';

  List<Json> _positions = [];
  List<Json> _departments = [];

  bool get _isEdit => widget.item != null;

  @override
  void initState() {
    super.initState();
    _positionId = widget.item?['positionId'] as int?;
    _departmentId = widget.item?['departmentId'] as int?;
    Future.wait([
      fetchList(widget.api, '/api/positions'),
      fetchList(widget.api, '/api/departments'),
    ]).then((r) {
      if (!mounted) return;
      setState(() {
        _positions = r[0];
        _departments = r[1];
      });
    }).catchError((Object e) {
      showError(e);
    });
  }

  @override
  void dispose() {
    for (final c in [_username, _password, _fullName, _email, _phone]) {
      c.dispose();
    }
    super.dispose();
  }

  /// Chức vụ thuộc nhóm của Role đang chọn (Admin không có chức vụ).
  List<Json> get _positionsForRole =>
      _positions.where((p) => p['roleGroup'] == _role).toList();

  void _onRoleChanged(String? v) {
    if (v == null || v == _role) return;
    setState(() {
      _role = v;
      _positionId = null;
    });
  }

  Future<void> _submit() async {
    final dio = widget.api.dio;
    final posId = _role == 'Admin' ? null : _positionId;
    if (_isEdit) {
      final id = widget.item!['id'];
      if (_role != widget.item!['role'] || posId != widget.item!['positionId']) {
        await dio.put('/api/employees/$id/role',
            data: {'role': _role, 'positionId': posId});
      }
      await dio.put('/api/employees/$id', data: {
        'fullName': _fullName.text.trim(),
        'email': emptyToNull(_email.text),
        'phone': emptyToNull(_phone.text),
        'departmentId': _departmentId,
        'status': _status,
        'newPassword': emptyToNull(_password.text),
      });
    } else {
      await dio.post('/api/employees', data: {
        'username': _username.text.trim(),
        'password': _password.text,
        'fullName': _fullName.text.trim(),
        'email': emptyToNull(_email.text),
        'phone': emptyToNull(_phone.text),
        'role': _role,
        'positionId': posId,
        'departmentId': _departmentId,
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final positions = _positionsForRole;
    final posIds = positions.map((p) => p['id']).toSet();
    final deptIds = _departments.map((d) => d['id']).toSet();
    return FormDialog(
      title: _isEdit ? 'Sửa nhân viên' : 'Thêm nhân viên',
      formKey: _key,
      onSubmit: _submit,
      children: [
        TextFormField(
          controller: _username,
          enabled: !_isEdit,
          decoration: const InputDecoration(labelText: 'Username'),
          validator: (v) => (v == null || v.trim().length < 3)
              ? 'Ít nhất 3 ký tự'
              : null,
        ),
        TextFormField(
          controller: _password,
          obscureText: true,
          decoration: InputDecoration(
              labelText: _isEdit ? 'Mật khẩu mới (bỏ trống = giữ nguyên)' : 'Mật khẩu tạm'),
          validator: (v) {
            if (_isEdit && (v == null || v.isEmpty)) return null;
            return (v == null || v.length < 6) ? 'Ít nhất 6 ký tự' : null;
          },
        ),
        TextFormField(
          controller: _fullName,
          decoration: const InputDecoration(labelText: 'Họ tên'),
          validator: notEmpty,
        ),
        TextFormField(
          controller: _email,
          keyboardType: TextInputType.emailAddress,
          decoration: const InputDecoration(labelText: 'Email'),
        ),
        TextFormField(
          controller: _phone,
          keyboardType: TextInputType.phone,
          decoration: const InputDecoration(labelText: 'Số điện thoại'),
          validator: (v) => (_email.text.trim().isEmpty && (v ?? '').trim().isEmpty)
              ? 'Nhập Email hoặc số điện thoại'
              : null,
        ),
        // Ô 1: Role
        DropdownButtonFormField<String>(
          key: const Key('role-dropdown'),
          initialValue: _role,
          decoration: const InputDecoration(labelText: 'Role'),
          items: [
            for (final e in roleLabels.entries)
              DropdownMenuItem(value: e.key, child: Text(e.value)),
          ],
          onChanged: _onRoleChanged,
        ),
        // Ô 2: Chức vụ, lọc theo ô 1; đổi Role thì tạo lại ô (ValueKey) để reset
        if (_role != 'Admin')
          DropdownButtonFormField<int>(
            key: ValueKey('pos-$_role-${_positions.length}'),
            initialValue: posIds.contains(_positionId) ? _positionId : null,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Chức vụ'),
            items: [
              for (final p in positions)
                DropdownMenuItem(
                    value: p['id'] as int, child: Text(p['name'] as String)),
            ],
            validator: (v) => v == null ? 'Chọn chức vụ' : null,
            onChanged: (v) => _positionId = v,
          ),
        DropdownButtonFormField<int?>(
          key: ValueKey('dept-${_departments.length}'),
          initialValue: deptIds.contains(_departmentId) ? _departmentId : null,
          isExpanded: true,
          decoration: const InputDecoration(labelText: 'Phòng ban'),
          items: [
            const DropdownMenuItem(value: null, child: Text('— Không có —')),
            for (final d in _departments)
              DropdownMenuItem(
                  value: d['id'] as int, child: Text(d['name'] as String)),
          ],
          onChanged: (v) => _departmentId = v,
        ),
        if (_isEdit)
          DropdownButtonFormField<String>(
            initialValue: _status,
            decoration: const InputDecoration(labelText: 'Trạng thái'),
            items: const [
              DropdownMenuItem(value: 'Active', child: Text('Hoạt động')),
              DropdownMenuItem(value: 'Locked', child: Text('Khóa')),
            ],
            onChanged: (v) => _status = v ?? _status,
          ),
      ],
    );
  }
}
