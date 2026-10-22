import 'package:flutter/material.dart';

/// Chỗ giữ cho màn đăng nhập khuôn mặt; Bước 15 thay bằng bản đầy đủ.
class FaceLoginPage extends StatelessWidget {
  const FaceLoginPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Đăng nhập bằng khuôn mặt')),
      body: const Center(child: Text('Sẽ làm ở Bước 15')),
    );
  }
}
