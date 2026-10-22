/// Máy trạng thái chớp mắt: mắt mở → nhắm → mở lại thì coi là người thật.
/// Đầu vào là xác suất mở mắt (0..1) của từng khung hình; null = không thấy mặt.
class BlinkDetector {
  static const openAbove = 0.7;
  static const closedBelow = 0.3;

  bool _sawOpen = false;
  bool _sawClosed = false;
  bool _done = false;

  bool get done => _done;

  /// Gợi ý cho người dùng ở bước hiện tại.
  String get hint {
    if (_done) return 'Đã nhận chớp mắt';
    if (!_sawOpen) return 'Nhìn thẳng vào camera, mở to mắt';
    if (!_sawClosed) return 'Hãy chớp mắt';
    return 'Mở mắt ra';
  }

  void reset() {
    _sawOpen = false;
    _sawClosed = false;
    _done = false;
  }

  /// Trả true khi vừa hoàn thành chuỗi mở → nhắm → mở.
  bool update(double? eyesOpen) {
    if (_done || eyesOpen == null) return false;
    if (!_sawOpen) {
      if (eyesOpen >= openAbove) _sawOpen = true;
    } else if (!_sawClosed) {
      if (eyesOpen <= closedBelow) _sawClosed = true;
    } else if (eyesOpen >= openAbove) {
      _done = true;
      return true;
    }
    return false;
  }
}
