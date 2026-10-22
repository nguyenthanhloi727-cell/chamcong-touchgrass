import 'package:dio/dio.dart';
import 'package:flutter/material.dart';

import '../config.dart';
import 'token_storage.dart';

/// Hộp thư SnackBar toàn app (gắn vào MaterialApp.scaffoldMessengerKey).
final GlobalKey<ScaffoldMessengerState> messengerKey =
    GlobalKey<ScaffoldMessengerState>();

/// Lấy thông báo lỗi từ `{ "error": "..." }` của backend.
String apiErrorMessage(Object error) {
  if (error is DioException) {
    final data = error.response?.data;
    if (data is Map && data['error'] is String) return data['error'] as String;
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.sendTimeout:
        return 'Hết thời gian chờ server';
      case DioExceptionType.connectionError:
        return 'Không kết nối được server ($baseUrl)';
      default:
        return error.message ?? 'Lỗi không xác định';
    }
  }
  return error.toString();
}

void showError(Object error) {
  messengerKey.currentState
    ?..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(apiErrorMessage(error))));
}

class ApiClient {
  final TokenStorage tokens;
  final Dio dio;

  /// Gọi khi refresh thất bại (phiên hết hạn) để đẩy người dùng về màn đăng nhập.
  VoidCallback? onSessionExpired;

  ApiClient(this.tokens)
      : dio = Dio(BaseOptions(
          baseUrl: baseUrl,
          connectTimeout: const Duration(seconds: 10),
          // SMS OTP có thể chờ gateway tới 15s
          receiveTimeout: const Duration(seconds: 30),
        )) {
    dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final token = await tokens.accessToken;
        if (token != null && options.headers['Authorization'] == null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        handler.next(options);
      },
      onError: (err, handler) async {
        final req = err.requestOptions;
        final isAuthCall = req.path.startsWith('/api/auth/');
        if (err.response?.statusCode == 401 &&
            !isAuthCall &&
            req.extra['retried'] != true &&
            await _refresh()) {
          try {
            req.extra['retried'] = true;
            req.headers.remove('Authorization');
            return handler.resolve(await dio.fetch(req));
          } on DioException catch (e) {
            return handler.next(e);
          }
        }
        handler.next(err);
      },
    ));
  }

  Future<bool>? _refreshing;

  Future<bool> _refresh() => _refreshing ??= _doRefresh().whenComplete(() {
        _refreshing = null;
      });

  Future<bool> _doRefresh() async {
    final refresh = await tokens.refreshToken;
    if (refresh == null) return false;
    try {
      final res = await Dio(BaseOptions(baseUrl: baseUrl)).post(
        '/api/auth/refresh',
        data: {'refreshToken': refresh},
      );
      await tokens.save(
        res.data['accessToken'] as String,
        res.data['refreshToken'] as String,
      );
      return true;
    } on DioException {
      await tokens.clear();
      onSessionExpired?.call();
      return false;
    }
  }

  Future<String> health() async {
    final res = await dio.get('/api/health');
    return res.data['status'] as String;
  }
}
