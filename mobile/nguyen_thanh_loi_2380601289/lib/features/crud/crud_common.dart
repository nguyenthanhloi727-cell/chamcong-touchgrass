import 'package:flutter/material.dart';

import '../../core/api/api_client.dart';

typedef Json = Map<String, dynamic>;

/// GET một danh sách JSON.
Future<List<Json>> fetchList(ApiClient api, String path, {Json? query}) async {
  final res = await api.dio.get(path, queryParameters: query);
  return (res.data as List).cast<Json>();
}

String? notEmpty(String? v, [String msg = 'Không được để trống']) =>
    (v == null || v.trim().isEmpty) ? msg : null;

String? emptyToNull(String s) => s.trim().isEmpty ? null : s.trim();

String hhmm(TimeOfDay t) =>
    '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

TimeOfDay parseTime(String s) => TimeOfDay(
      hour: int.parse(s.substring(0, 2)),
      minute: int.parse(s.substring(3, 5)),
    );

Future<bool> confirmDelete(BuildContext context, String what) async {
  final ok = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: const Text('Xác nhận xóa'),
      content: Text('Xóa "$what"?'),
      actions: [
        TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Hủy')),
        FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Xóa')),
      ],
    ),
  );
  return ok ?? false;
}

/// Khung hộp thoại thêm/sửa: kiểm tra form, khóa nút khi đang gửi, hiện lỗi `{error}` ngay trong hộp.
/// Đóng bằng `true` khi [onSubmit] thành công.
class FormDialog extends StatefulWidget {
  final String title;
  final GlobalKey<FormState> formKey;
  final List<Widget> children;
  final Future<void> Function() onSubmit;

  const FormDialog({
    super.key,
    required this.title,
    required this.formKey,
    required this.children,
    required this.onSubmit,
  });

  @override
  State<FormDialog> createState() => _FormDialogState();
}

class _FormDialogState extends State<FormDialog> {
  bool _busy = false;
  String? _error;

  Future<void> _submit() async {
    if (_busy || !widget.formKey.currentState!.validate()) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await widget.onSubmit();
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) {
        setState(() {
          _busy = false;
          _error = apiErrorMessage(e);
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(widget.title),
      content: SizedBox(
        width: double.maxFinite,
        child: SingleChildScrollView(
          child: Form(
            key: widget.formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                for (final c in widget.children)
                  Padding(padding: const EdgeInsets.only(bottom: 12), child: c),
                if (_error != null)
                  Text(_error!,
                      key: const Key('form-error'),
                      style: TextStyle(
                          color: Theme.of(context).colorScheme.error)),
              ],
            ),
          ),
        ),
      ),
      actions: [
        TextButton(
            onPressed: _busy ? null : () => Navigator.pop(context, false),
            child: const Text('Hủy')),
        FilledButton(
          onPressed: _busy ? null : _submit,
          child: _busy
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(strokeWidth: 2))
              : const Text('Lưu'),
        ),
      ],
    );
  }
}

/// Màn danh sách dùng chung: ô tìm kiếm, bấm để sửa, nút xóa (có hộp xác nhận), nút Thêm.
class CrudListPage extends StatefulWidget {
  final String title;
  final bool searchable;
  final Future<List<Json>> Function(String q) load;
  final String Function(Json) titleOf;
  final String Function(Json) subtitleOf;

  /// Mở form thêm (item = null) hoặc sửa; trả true nếu có thay đổi.
  final Future<bool?> Function(BuildContext context, Json? item) form;
  final Future<void> Function(Json item) onDelete;
  final bool canAdd;
  final bool canDelete;
  final Widget? header;
  final Widget? Function(Json)? trailingOf;

  const CrudListPage({
    super.key,
    required this.title,
    required this.load,
    required this.titleOf,
    required this.subtitleOf,
    required this.form,
    required this.onDelete,
    this.searchable = true,
    this.canAdd = true,
    this.canDelete = true,
    this.header,
    this.trailingOf,
  });

  @override
  State<CrudListPage> createState() => _CrudListPageState();
}

class _CrudListPageState extends State<CrudListPage> {
  final _search = TextEditingController();
  late Future<List<Json>> _data = widget.load('');

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _reload() async {
    final f = widget.load(_search.text.trim());
    setState(() {
      _data = f;
    });
    try {
      await f;
    } catch (_) {}
  }

  Future<void> _openForm(Json? item) async {
    final changed = await widget.form(context, item);
    if (changed == true) _reload();
  }

  Future<void> _delete(Json item) async {
    if (!await confirmDelete(context, widget.titleOf(item))) return;
    try {
      await widget.onDelete(item);
      _reload();
    } catch (e) {
      showError(e);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.title)),
      floatingActionButton: widget.canAdd
          ? FloatingActionButton(
              tooltip: 'Thêm',
              onPressed: () => _openForm(null),
              child: const Icon(Icons.add),
            )
          : null,
      body: Column(
        children: [
          if (widget.header != null) widget.header!,
          if (widget.searchable)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
              child: TextField(
                controller: _search,
                textInputAction: TextInputAction.search,
                decoration: InputDecoration(
                  hintText: 'Tìm kiếm',
                  prefixIcon: const Icon(Icons.search),
                  border: const OutlineInputBorder(),
                  isDense: true,
                  suffixIcon: IconButton(
                    tooltip: 'Xóa tìm kiếm',
                    icon: const Icon(Icons.clear),
                    onPressed: () {
                      _search.clear();
                      _reload();
                    },
                  ),
                ),
                onSubmitted: (_) => _reload(),
              ),
            ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _reload,
              child: FutureBuilder<List<Json>>(
                future: _data,
                builder: (context, snap) {
                  if (snap.connectionState != ConnectionState.done) {
                    return const Center(child: CircularProgressIndicator());
                  }
                  if (snap.hasError) {
                    return ListView(
                      padding: const EdgeInsets.all(16),
                      children: [
                        Text(
                            'Không tải được dữ liệu. Kéo xuống để thử lại.\n${apiErrorMessage(snap.error!)}')
                      ],
                    );
                  }
                  final items = snap.data!;
                  if (items.isEmpty) {
                    return ListView(
                      padding: const EdgeInsets.all(32),
                      children: const [Center(child: Text('Chưa có dữ liệu'))],
                    );
                  }
                  return ListView.separated(
                    padding: const EdgeInsets.only(bottom: 88),
                    itemCount: items.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, i) {
                      final item = items[i];
                      return ListTile(
                        title: Text(widget.titleOf(item)),
                        subtitle: Text(widget.subtitleOf(item)),
                        onTap: () => _openForm(item),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            if (widget.trailingOf?.call(item) != null)
                              widget.trailingOf!(item)!,
                            if (widget.canDelete)
                              IconButton(
                                tooltip: 'Xóa',
                                icon: const Icon(Icons.delete_outline),
                                onPressed: () => _delete(item),
                              ),
                          ],
                        ),
                      );
                    },
                  );
                },
              ),
            ),
          ),
        ],
      ),
    );
  }
}
