// Small JSON-over-HTTP helper for Instafuel.Api. Adds the bearer token and
// turns failed responses into AppException with a message for the user.

import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../repositories.dart';

class ApiClient {
  ApiClient({required this.baseUrl, http.Client? httpClient}) : _http = httpClient ?? http.Client();

  final String baseUrl;
  final http.Client _http;

  /// JWT from login/register. Set by ApiAuthRepository, cleared on logout.
  String? token;

  /// Called when a request with a token gets 401 (expired/invalid token).
  /// AuthController sets this to log the user out.
  void Function()? onUnauthorized;

  Future<dynamic> get(String path) => _send('GET', path);
  Future<dynamic> post(String path, Object? body) => _send('POST', path, body);
  Future<dynamic> put(String path, Object? body) => _send('PUT', path, body);
  Future<dynamic> delete(String path) => _send('DELETE', path);

  /// multipart/form-data upload of one file in the "file" field.
  Future<dynamic> upload(String path, {required List<int> bytes, required String fileName}) async {
    final request = http.MultipartRequest('POST', Uri.parse('$baseUrl$path'))
      ..headers['Accept'] = 'application/json'
      ..files.add(http.MultipartFile.fromBytes('file', bytes, filename: fileName));
    if (token != null) request.headers['Authorization'] = 'Bearer $token';
    return _handle(request, timeout: const Duration(seconds: 60));
  }

  /// Raw bytes of a file endpoint (e.g. an uploaded document preview).
  Future<Uint8List> getBytes(String path) async {
    final request = http.Request('GET', Uri.parse('$baseUrl$path'));
    if (token != null) request.headers['Authorization'] = 'Bearer $token';
    final response = await _execute(request, const Duration(seconds: 30));
    if (response.statusCode >= 200 && response.statusCode < 300) return response.bodyBytes;
    if (response.statusCode == 401) onUnauthorized?.call();
    throw AppException(errorMessage(response.statusCode, _decode(response.body)));
  }

  Future<dynamic> _send(String method, String path, [Object? body]) async {
    final request = http.Request(method, Uri.parse('$baseUrl$path'))..headers['Accept'] = 'application/json';
    if (token != null) request.headers['Authorization'] = 'Bearer $token';
    if (body != null) {
      request.headers['Content-Type'] = 'application/json';
      request.body = jsonEncode(body);
    }

    return _handle(request);
  }

  Future<dynamic> _handle(http.BaseRequest request, {Duration timeout = const Duration(seconds: 15)}) async {
    final response = await _execute(request, timeout);
    final data = _decode(response.body);
    if (response.statusCode >= 200 && response.statusCode < 300) return data;
    if (response.statusCode == 401 && request.headers.containsKey('Authorization')) onUnauthorized?.call();
    throw AppException(errorMessage(response.statusCode, data));
  }

  Future<http.Response> _execute(http.BaseRequest request, Duration timeout) async {
    try {
      final streamed = await _http.send(request).timeout(timeout);
      return await http.Response.fromStream(streamed);
    } on TimeoutException {
      throw const AppException('The server took too long to respond. Please try again.');
    } on http.ClientException {
      throw const AppException("Can't reach the server. Check your connection and try again.");
    }
  }

  static dynamic _decode(String body) {
    if (body.isEmpty) return null;
    try {
      return jsonDecode(body);
    } on FormatException {
      return null; // not JSON (e.g. a proxy error page)
    }
  }

  /// The API sends `{ "message": ... }` for its own errors and ASP.NET
  /// validation problems as `{ "errors": { "Field": ["..."] } }`.
  static String errorMessage(int statusCode, dynamic data) {
    if (data is Map) {
      final message = data['message'];
      if (message is String && message.isNotEmpty) return message;

      final errors = data['errors'];
      if (errors is Map && errors.isNotEmpty) {
        final first = errors.values.first;
        if (first is List && first.isNotEmpty) return '${first.first}';
      }
    }
    if (statusCode == 401) return 'Your session has expired. Please log in again.';
    return 'Something went wrong (error $statusCode). Please try again.';
  }
}
