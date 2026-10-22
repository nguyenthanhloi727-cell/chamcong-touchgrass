import 'dart:async';

import 'package:camera/camera.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_mlkit_face_detection/google_mlkit_face_detection.dart';

import 'blink_detector.dart';
import 'face_embedding.dart';

/// Mở camera trước, bắt buộc chớp mắt trong [timeout] rồi trả embedding
/// (null nếu người dùng thoát). Dùng cho đăng ký, đăng nhập và chấm công.
class FaceCapturePage extends StatefulWidget {
  final String title;
  final Duration timeout;
  const FaceCapturePage(
      {super.key,
      this.title = 'Xác minh khuôn mặt',
      this.timeout = const Duration(seconds: 5)});

  /// Mở màn chụp; trả embedding hoặc null.
  static Future<List<double>?> open(BuildContext context,
      {String title = 'Xác minh khuôn mặt'}) {
    return Navigator.of(context).push<List<double>>(
        MaterialPageRoute(builder: (_) => FaceCapturePage(title: title)));
  }

  @override
  State<FaceCapturePage> createState() => _FaceCapturePageState();
}

class _FaceCapturePageState extends State<FaceCapturePage> {
  CameraController? _camera;
  final _detector = FaceDetector(
    options: FaceDetectorOptions(
      enableClassification: true,
      enableContours: true,
      performanceMode: FaceDetectorMode.fast,
    ),
  );
  final _blink = BlinkDetector();
  Timer? _timer;
  bool _processing = false;
  bool _finished = false;
  bool _timedOut = false;
  bool _faceSeen = false;
  String? _error;
  List<double>? _lastEmbedding;
  int _secondsLeft = 0;

  @override
  void initState() {
    super.initState();
    SystemChrome.setPreferredOrientations([DeviceOrientation.portraitUp]);
    _init();
  }

  Future<void> _init() async {
    try {
      final cams = await availableCameras();
      final front = cams.firstWhere(
          (c) => c.lensDirection == CameraLensDirection.front,
          orElse: () => cams.first);
      final controller = CameraController(front, ResolutionPreset.medium,
          enableAudio: false, imageFormatGroup: ImageFormatGroup.nv21);
      await controller.initialize();
      if (!mounted) {
        await controller.dispose();
        return;
      }
      setState(() => _camera = controller);
      _start();
    } on CameraException catch (e) {
      if (mounted) {
        setState(() => _error = e.code == 'CameraAccessDenied'
            ? 'Chưa được cấp quyền camera'
            : 'Không mở được camera: ${e.description ?? e.code}');
      }
    } catch (e) {
      if (mounted) setState(() => _error = 'Không mở được camera: $e');
    }
  }

  void _start() {
    _blink.reset();
    _lastEmbedding = null;
    _timedOut = false;
    _faceSeen = false;
    _secondsLeft = widget.timeout.inSeconds;
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(seconds: 1), (t) {
      if (!mounted) return;
      setState(() => _secondsLeft--);
      if (_secondsLeft <= 0) {
        t.cancel();
        _stopStream();
        setState(() => _timedOut = true);
      }
    });
    _camera!.startImageStream(_onFrame);
  }

  Future<void> _stopStream() async {
    final c = _camera;
    if (c != null && c.value.isStreamingImages) {
      try {
        await c.stopImageStream();
      } catch (_) {}
    }
  }

  Future<void> _onFrame(CameraImage image) async {
    if (_processing || _finished || _timedOut) return;
    _processing = true;
    try {
      final input = _toInputImage(image);
      if (input == null) return;
      final faces = await _detector.processImage(input);
      if (!mounted || _finished) return;
      if (faces.length != 1) {
        if (_faceSeen) setState(() => _faceSeen = false);
        return;
      }
      final face = faces.first;
      if (!_faceSeen) setState(() => _faceSeen = true);
      final emb = faceEmbedding(face);
      if (emb != null) _lastEmbedding = emb;
      final before = _blink.hint;
      if (_blink.update(eyesOpenProbability(face)) && _lastEmbedding != null) {
        _finished = true;
        _timer?.cancel();
        await _stopStream();
        if (mounted) Navigator.pop(context, _lastEmbedding);
        return;
      }
      if (before != _blink.hint) setState(() {});
    } finally {
      _processing = false;
    }
  }

  InputImage? _toInputImage(CameraImage image) {
    final cam = _camera!.description;
    final rotation =
        InputImageRotationValue.fromRawValue(cam.sensorOrientation);
    final format = InputImageFormatValue.fromRawValue(image.format.raw);
    if (rotation == null ||
        format == null ||
        format != InputImageFormat.nv21 ||
        image.planes.length != 1) {
      return null;
    }
    final plane = image.planes.first;
    return InputImage.fromBytes(
      bytes: plane.bytes,
      metadata: InputImageMetadata(
        size: Size(image.width.toDouble(), image.height.toDouble()),
        rotation: rotation,
        format: format,
        bytesPerRow: plane.bytesPerRow,
      ),
    );
  }

  @override
  void dispose() {
    _timer?.cancel();
    SystemChrome.setPreferredOrientations([]);
    final c = _camera;
    if (c != null) {
      _stopStream().whenComplete(c.dispose);
    }
    _detector.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final cam = _camera;
    return Scaffold(
      appBar: AppBar(title: Text(widget.title)),
      body: _error != null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(_error!, textAlign: TextAlign.center),
              ),
            )
          : cam == null
              ? const Center(child: CircularProgressIndicator())
              : Column(
                  children: [
                    Expanded(
                      child: Center(
                        child: AspectRatio(
                          aspectRatio: 1 / cam.value.aspectRatio,
                          child: CameraPreview(cam),
                        ),
                      ),
                    ),
                    Padding(
                      padding: const EdgeInsets.all(16),
                      child: _timedOut ? _retry() : _status(),
                    ),
                  ],
                ),
    );
  }

  Widget _status() {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(_faceSeen ? _blink.hint : 'Đưa khuôn mặt vào khung hình',
            key: const Key('face-hint'),
            style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 8),
        Text('Còn $_secondsLeft giây'),
      ],
    );
  }

  Widget _retry() {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        const Text('Không thấy chớp mắt. Cần mặt thật, mắt mở rồi chớp.',
            textAlign: TextAlign.center),
        const SizedBox(height: 8),
        FilledButton(
            onPressed: () => setState(_start), child: const Text('Thử lại')),
      ],
    );
  }
}
