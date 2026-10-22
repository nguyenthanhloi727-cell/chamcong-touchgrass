import 'package:flutter/material.dart';

import 'animations.dart';

/// Chữ cái đầu của tên (tối đa 2 ký tự) cho ảnh đại diện.
String initials(String name) {
  // "Quản trị viên (admin)" → bỏ phần trong ngoặc, chỉ lấy chữ cái của tên.
  final plain = name.replaceAll(RegExp(r'\(.*?\)'), '');
  final parts = plain.trim().split(RegExp(r'\s+')).where((p) => p.isNotEmpty);
  if (parts.isEmpty) return '?';
  final list = parts.toList();
  final first = list.first[0];
  final last = list.length > 1 ? list.last[0] : '';
  return (first + last).toUpperCase();
}

/// Tròn có chữ cái đầu.
class InitialsAvatar extends StatelessWidget {
  final String name;
  final double radius;
  final Color? background;
  final Color? foreground;
  const InitialsAvatar({
    super.key,
    required this.name,
    this.radius = 22,
    this.background,
    this.foreground,
  });

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return CircleAvatar(
      radius: radius,
      backgroundColor: background ?? scheme.primaryContainer,
      foregroundColor: foreground ?? scheme.onPrimaryContainer,
      child: Text(
        initials(name),
        style: TextStyle(
          fontWeight: FontWeight.w700,
          fontSize: radius * 0.72,
        ),
      ),
    );
  }
}

/// Icon trong ô bo góc có nền màu nhạt.
class IconBadge extends StatelessWidget {
  final IconData icon;
  final Color color;
  final double size;
  const IconBadge({
    super.key,
    required this.icon,
    required this.color,
    this.size = 44,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.14),
        borderRadius: BorderRadius.circular(size * 0.32),
      ),
      child: Icon(icon, color: color, size: size * 0.52),
    );
  }
}

/// Nhãn trạng thái bo tròn.
class StatusPill extends StatelessWidget {
  final String label;
  final Color color;
  const StatusPill({super.key, required this.label, required this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.14),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontSize: 12.5,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

/// Tiêu đề từng khối nội dung.
class SectionTitle extends StatelessWidget {
  final String text;
  final Widget? trailing;
  const SectionTitle(this.text, {super.key, this.trailing});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(4, 20, 4, 8),
      child: Row(
        children: [
          Expanded(
            child: Text(
              text,
              style: Theme.of(context)
                  .textTheme
                  .titleMedium
                  ?.copyWith(fontWeight: FontWeight.w700),
            ),
          ),
          ?trailing,
        ],
      ),
    );
  }
}

/// Trạng thái rỗng: icon lớn + dòng chữ.
class EmptyState extends StatelessWidget {
  final IconData icon;
  final String text;
  const EmptyState({
    super.key,
    this.icon = Icons.inbox_outlined,
    required this.text,
  });

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return FadeSlideIn(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 28, horizontal: 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 64,
              height: 64,
              decoration: BoxDecoration(
                color: scheme.surfaceContainerHighest.withValues(alpha: 0.6),
                shape: BoxShape.circle,
              ),
              child: Icon(icon, size: 30, color: scheme.onSurfaceVariant),
            ),
            const SizedBox(height: 12),
            Text(
              text,
              textAlign: TextAlign.center,
              style: Theme.of(context)
                  .textTheme
                  .bodyLarge
                  ?.copyWith(color: scheme.onSurfaceVariant),
            ),
          ],
        ),
      ),
    );
  }
}

/// Màu theo trạng thái chấm công (OnTime | Late | PendingReview | Rejected).
Color statusColor(String status) => switch (status) {
      'OnTime' => const Color(0xFF16A34A),
      'Late' => const Color(0xFFEA580C),
      'PendingReview' => const Color(0xFF2563EB),
      'Rejected' => const Color(0xFFDC2626),
      _ => const Color(0xFF6B7280),
    };

/// Màu theo điểm tin cậy: từ 70 điểm trở lên là đạt.
Color trustColor(int score) =>
    score >= 70 ? const Color(0xFF16A34A) : const Color(0xFFEA580C);
