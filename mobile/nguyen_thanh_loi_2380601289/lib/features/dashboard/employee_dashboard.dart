import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/widgets/animations.dart';
import '../../core/widgets/ui.dart';
import '../auth/session_store.dart';
import 'dashboard_api.dart';
import 'dashboard_models.dart';
import 'dashboard_shell.dart';

/// Dashboard Nhân viên: nút Chấm công, ca hôm nay, lịch sử tháng, điểm tin cậy gần nhất.
class EmployeeDashboard extends StatefulWidget {
  final SessionStore session;
  const EmployeeDashboard({super.key, required this.session});

  @override
  State<EmployeeDashboard> createState() => _EmployeeDashboardState();
}

class _EmployeeDashboardState extends State<EmployeeDashboard> {
  late final DashboardApi _api = DashboardApi(widget.session.api);
  late Future<(List<ShiftItem>, List<AttendanceItem>)> _data = _load();

  Future<(List<ShiftItem>, List<AttendanceItem>)> _load() async {
    final now = DateTime.now();
    final r = await Future.wait([
      _api.shifts(),
      _api.attendances(from: DateTime(now.year, now.month, 1), to: now),
    ]);
    return (r[0] as List<ShiftItem>, r[1] as List<AttendanceItem>);
  }

  Future<void> _reload() async {
    final f = _load();
    setState(() {
      _data = f;
    });
    try {
      await f;
    } catch (_) {}
  }

  /// Ca đang diễn ra; không có thì ca bắt đầu sớm nhất.
  ShiftItem? _todayShift(List<ShiftItem> shifts) {
    if (shifts.isEmpty) return null;
    final now = '${hm(DateTime.now())}:00';
    for (final s in shifts) {
      if (s.start.compareTo(now) <= 0 && now.compareTo(s.end) <= 0) return s;
    }
    return ([...shifts]..sort((a, b) => a.start.compareTo(b.start))).first;
  }

  String _day(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}';

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return DashboardShell(
      session: widget.session,
      title: 'Nhân viên',
      onRefresh: _reload,
      body: AsyncSection(
        future: _data,
        builder: (data) {
          final (shifts, history) = data;
          final shift = _todayShift(shifts);
          final last = history.isEmpty ? null : history.first; // API sắp mới → cũ
          return ListView(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
            children: [
              ListenableBuilder(
                listenable: widget.session,
                builder: (context, _) =>
                    (widget.session.user?.hasFace ?? false)
                        ? const SizedBox.shrink()
                        : FadeSlideIn(
                            child: _FaceBanner(
                                onTap: () => context.push('/face-enroll')),
                          ),
              ),
              FadeSlideIn(
                index: 1,
                child: _CheckinButton(
                  onPressed: () async {
                    await context.push('/employee/checkin');
                    _reload();
                  },
                ),
              ),
              const SizedBox(height: 16),
              FadeSlideIn(
                index: 1,
                child: Row(
                  children: [
                    Expanded(
                      child: _InfoTile(
                        icon: Icons.schedule_rounded,
                        color: const Color(0xFFEA580C),
                        title: 'Ca hôm nay',
                        value: shift?.label ?? 'Chưa có ca làm',
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _InfoTile(
                        icon: Icons.verified_user_rounded,
                        color: last == null
                            ? scheme.outline
                            : trustColor(last.trustScore),
                        title: 'Điểm tin cậy lần gần nhất',
                        value: last == null
                            ? 'Chưa có lần chấm công nào'
                            : '${last.trustScore}/100 · ${statusLabel(last.status)}',
                      ),
                    ),
                  ],
                ),
              ),
              FadeSlideIn(
                index: 2,
                child: ListenableBuilder(
                  listenable: widget.session,
                  builder: (context, _) =>
                      (widget.session.user?.hasFace ?? false)
                          ? _FaceCard(
                              registered: true,
                              onTap: () => context.push('/face-enroll'),
                            )
                          : const SizedBox.shrink(),
                ),
              ),
              const SectionTitle('Lịch sử chấm công tháng này'),
              if (history.isEmpty)
                const EmptyState(
                    icon: Icons.event_busy_outlined, text: 'Chưa có dữ liệu'),
              for (var i = 0; i < history.length; i++)
                FadeSlideIn(
                  index: i + 3,
                  child: _HistoryTile(item: history[i], day: _day(history[i].checkInAt)),
                ),
            ],
          );
        },
      ),
    );
  }
}

class _CheckinButton extends StatelessWidget {
  final VoidCallback onPressed;
  const _CheckinButton({required this.onPressed});

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return PressScale(
      scale: 0.97,
      child: PulseRing(
        color: scheme.primary,
        borderRadius: 28,
        child: Material(
          color: Colors.transparent,
          borderRadius: BorderRadius.circular(28),
          child: Ink(
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(28),
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [
                  scheme.primary,
                  Color.lerp(scheme.primary, const Color(0xFF0B3D2A), 0.55)!,
                ],
              ),
            ),
            child: InkWell(
              borderRadius: BorderRadius.circular(28),
              onTap: onPressed,
              // Cao tối thiểu 112, tự giãn khi chữ xuống dòng (font hệ thống lớn / màn nhỏ).
              child: Container(
                constraints: const BoxConstraints(minHeight: 112),
                alignment: Alignment.center,
                padding:
                    const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
                child: Row(
                  children: [
                    Container(
                      width: 64,
                      height: 64,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.18),
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(Icons.fingerprint,
                          size: 40, color: Colors.white),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Chấm công',
                              style: TextStyle(
                                  color: Colors.white,
                                  fontSize: 26,
                                  fontWeight: FontWeight.w800)),
                          Text('Vị trí → Quét QR → Gửi',
                              style: TextStyle(
                                  color: Colors.white.withValues(alpha: 0.8))),
                        ],
                      ),
                    ),
                    const Icon(Icons.arrow_forward_rounded, color: Colors.white),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// Banner đầu dashboard khi nhân viên chưa đăng ký khuôn mặt: kêu gọi + nút Đăng ký ngay.
class _FaceBanner extends StatelessWidget {
  final VoidCallback onTap;
  const _FaceBanner({required this.onTap});

  @override
  Widget build(BuildContext context) {
    const color = Color(0xFFEA580C);
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Card(
        key: const Key('face-banner'),
        margin: EdgeInsets.zero,
        color: color.withValues(alpha: 0.08),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(20),
          side: BorderSide(color: color.withValues(alpha: 0.35)),
        ),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const IconBadge(
                      icon: Icons.face_retouching_natural, color: color, size: 48),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Đăng ký khuôn mặt',
                            style: Theme.of(context)
                                .textTheme
                                .titleMedium
                                ?.copyWith(fontWeight: FontWeight.w800)),
                        Text('Đăng nhập nhanh, không cần mã OTP',
                            style: TextStyle(
                                color: Theme.of(context)
                                    .colorScheme
                                    .onSurfaceVariant)),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 14),
              FilledButton.icon(
                key: const Key('face-banner-button'),
                style: FilledButton.styleFrom(
                    backgroundColor: color,
                    minimumSize: const Size.fromHeight(46)),
                onPressed: onTap,
                icon: const Icon(Icons.face),
                label: const Text('Đăng ký ngay'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Ô quản lý khuôn mặt: cho biết đã đăng ký chưa và dẫn tới màn quản lý.
class _FaceCard extends StatelessWidget {
  final bool registered;
  final VoidCallback onTap;
  const _FaceCard({required this.registered, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final color =
        registered ? const Color(0xFF16A34A) : const Color(0xFFEA580C);
    return Padding(
      padding: const EdgeInsets.only(top: 12),
      child: PressScale(
        child: Card(
          margin: EdgeInsets.zero,
          clipBehavior: Clip.antiAlias,
          child: InkWell(
            onTap: onTap,
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: Row(
                children: [
                  IconBadge(
                      icon: registered
                          ? Icons.face_rounded
                          : Icons.face_retouching_natural,
                      color: color),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Khuôn mặt',
                            style: Theme.of(context)
                                .textTheme
                                .titleSmall
                                ?.copyWith(fontWeight: FontWeight.w700)),
                        Text(
                          registered
                              ? 'Đã đăng ký · bấm để quản lý'
                              : 'Chưa đăng ký · bấm để đăng ký đăng nhập nhanh',
                          style: TextStyle(
                              color:
                                  Theme.of(context).colorScheme.onSurfaceVariant),
                        ),
                      ],
                    ),
                  ),
                  StatusPill(
                      label: registered ? 'Đã đăng ký' : 'Chưa có', color: color),
                  const SizedBox(width: 4),
                  const Icon(Icons.chevron_right_rounded),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _InfoTile extends StatelessWidget {
  final IconData icon;
  final Color color;
  final String title;
  final String value;
  const _InfoTile({
    required this.icon,
    required this.color,
    required this.title,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    final text = Theme.of(context).textTheme;
    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            IconBadge(icon: icon, color: color, size: 38),
            const SizedBox(height: 10),
            Text(title,
                style: text.labelMedium
                    ?.copyWith(color: Theme.of(context).colorScheme.onSurfaceVariant)),
            const SizedBox(height: 2),
            Text(value,
                style: text.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
          ],
        ),
      ),
    );
  }
}

class _HistoryTile extends StatelessWidget {
  final AttendanceItem item;
  final String day;
  const _HistoryTile({required this.item, required this.day});

  @override
  Widget build(BuildContext context) {
    final color = statusColor(item.status);
    return Card(
      child: ListTile(
        contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
        leading: Container(
          width: 46,
          height: 46,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.12),
            borderRadius: BorderRadius.circular(14),
          ),
          child: Text(day.substring(0, 2),
              style: TextStyle(
                  color: color, fontWeight: FontWeight.w800, fontSize: 18)),
        ),
        title: Text('$day · ${hm(item.checkInAt)}'
            '${item.checkOutAt != null ? ' - ${hm(item.checkOutAt!)}' : ''}'),
        subtitle: Text('Điểm ${item.trustScore}'),
        trailing: StatusPill(label: statusLabel(item.status), color: color),
      ),
    );
  }
}
