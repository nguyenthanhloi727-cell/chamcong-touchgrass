import 'package:flutter/material.dart';

bool _reduceMotion(BuildContext context) =>
    MediaQuery.maybeOf(context)?.disableAnimations ?? false;

/// Hiện dần + trượt nhẹ từ dưới lên. Phần tử có [index] lớn hiện chậm hơn một chút
/// nên một danh sách tự xuất hiện lần lượt (stagger) mà không cần điều phối.
class FadeSlideIn extends StatelessWidget {
  final Widget child;
  final int index;
  final double offset;
  const FadeSlideIn({
    super.key,
    required this.child,
    this.index = 0,
    this.offset = 18,
  });

  @override
  Widget build(BuildContext context) {
    if (_reduceMotion(context)) return child;
    final ms = 380 + (index.clamp(0, 12) * 70);
    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0, end: 1),
      duration: Duration(milliseconds: ms),
      curve: Curves.easeOutCubic,
      child: child,
      builder: (context, v, child) => Opacity(
        opacity: v.clamp(0.0, 1.0),
        child: Transform.translate(
          offset: Offset(0, offset * (1 - v)),
          child: child,
        ),
      ),
    );
  }
}

/// Thu nhỏ nhẹ khi đang chạm; không chặn sự kiện bấm của con.
class PressScale extends StatefulWidget {
  final Widget child;
  final double scale;
  const PressScale({super.key, required this.child, this.scale = 0.96});

  @override
  State<PressScale> createState() => _PressScaleState();
}

class _PressScaleState extends State<PressScale> {
  bool _down = false;

  void _set(bool v) {
    if (_down != v) setState(() => _down = v);
  }

  @override
  Widget build(BuildContext context) {
    return Listener(
      onPointerDown: (_) => _set(true),
      onPointerUp: (_) => _set(false),
      onPointerCancel: (_) => _set(false),
      child: AnimatedScale(
        scale: _down ? widget.scale : 1,
        duration: const Duration(milliseconds: 120),
        curve: Curves.easeOut,
        child: widget.child,
      ),
    );
  }
}

/// Số chạy từ 0 lên [value].
class CountUp extends StatelessWidget {
  final int value;
  final TextStyle? style;
  final String suffix;
  final Key? textKey;
  const CountUp({
    super.key,
    required this.value,
    this.style,
    this.suffix = '',
    this.textKey,
  });

  @override
  Widget build(BuildContext context) {
    if (_reduceMotion(context)) {
      return Text('$value$suffix', key: textKey, style: style);
    }
    return TweenAnimationBuilder<int>(
      tween: IntTween(begin: 0, end: value),
      duration: const Duration(milliseconds: 900),
      curve: Curves.easeOutCubic,
      builder: (context, v, _) => Text('$v$suffix', key: textKey, style: style),
    );
  }
}

/// Khối xám nhấp nháy dùng làm khung chờ khi đang tải dữ liệu.
class SkeletonBox extends StatefulWidget {
  final double height;
  final double? width;
  final double radius;
  const SkeletonBox({
    super.key,
    this.height = 72,
    this.width,
    this.radius = 16,
  });

  @override
  State<SkeletonBox> createState() => _SkeletonBoxState();
}

class _SkeletonBoxState extends State<SkeletonBox>
    with SingleTickerProviderStateMixin {
  late final AnimationController _c = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 900),
  )..repeat(reverse: true);

  @override
  void dispose() {
    _c.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final base = Theme.of(context).colorScheme.surfaceContainerHighest;
    return AnimatedBuilder(
      animation: _c,
      builder: (context, _) => Container(
        height: widget.height,
        width: widget.width,
        decoration: BoxDecoration(
          color: base.withValues(alpha: 0.45 + 0.4 * _c.value),
          borderRadius: BorderRadius.circular(widget.radius),
        ),
      ),
    );
  }
}

/// Danh sách khung chờ.
class SkeletonList extends StatelessWidget {
  final int count;
  final double itemHeight;
  const SkeletonList({super.key, this.count = 6, this.itemHeight = 72});

  @override
  Widget build(BuildContext context) {
    return ListView(
      physics: const NeverScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      children: [
        for (var i = 0; i < count; i++)
          Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: SkeletonBox(height: itemHeight),
          ),
      ],
    );
  }
}

/// Nhịp "thở" hữu hạn (không lặp vô hạn để không làm treo test hoặc tốn pin):
/// vòng sáng nở ra rồi tan, lặp [count] lần.
class PulseRing extends StatefulWidget {
  final Widget child;
  final Color color;
  final double borderRadius;
  final int count;
  const PulseRing({
    super.key,
    required this.child,
    required this.color,
    this.borderRadius = 28,
    this.count = 3,
  });

  @override
  State<PulseRing> createState() => _PulseRingState();
}

class _PulseRingState extends State<PulseRing>
    with SingleTickerProviderStateMixin {
  late final AnimationController _c = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1600),
  );

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (!_reduceMotion(context) && !_c.isAnimating && _c.value == 0) {
      _c.repeat(count: widget.count);
    }
  }

  @override
  void dispose() {
    _c.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _c,
      child: widget.child,
      builder: (context, child) {
        final t = Curves.easeOut.transform(_c.value);
        return DecoratedBox(
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(widget.borderRadius),
            boxShadow: [
              BoxShadow(
                color: widget.color.withValues(alpha: 0.45 * (1 - t)),
                blurRadius: 4 + 26 * t,
                spreadRadius: 10 * t,
              ),
            ],
          ),
          child: child,
        );
      },
    );
  }
}
