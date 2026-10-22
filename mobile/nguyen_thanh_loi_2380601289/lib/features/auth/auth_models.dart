class UserInfo {
  final int id;
  final String username;
  final String fullName;
  final String? email;
  final bool emailVerified;
  final String? phoneNumber;
  final bool phoneVerified;
  final String role;
  final String? position;
  final bool hasFace;

  const UserInfo({
    required this.id,
    required this.username,
    required this.fullName,
    this.email,
    this.emailVerified = false,
    this.phoneNumber,
    this.phoneVerified = false,
    required this.role,
    this.position,
    this.hasFace = false,
  });

  factory UserInfo.fromJson(Map<String, dynamic> j) => UserInfo(
        id: j['id'] as int,
        username: j['username'] as String,
        fullName: j['fullName'] as String,
        email: j['email'] as String?,
        emailVerified: j['emailVerified'] as bool? ?? false,
        phoneNumber: j['phoneNumber'] as String?,
        phoneVerified: j['phoneVerified'] as bool? ?? false,
        role: j['role'] as String,
        position: j['position'] as String?,
        hasFace: j['hasFace'] as bool? ?? false,
      );
}

/// Kết quả register / login: phiên OTP vừa tạo.
class OtpSession {
  final int id;
  final String channel; // "Email" | "Sms"
  final String maskedDestination;

  const OtpSession({
    required this.id,
    required this.channel,
    required this.maskedDestination,
  });

  factory OtpSession.fromJson(Map<String, dynamic> j) => OtpSession(
        id: j['otpSessionId'] as int,
        channel: j['channel'] as String,
        maskedDestination: j['maskedDestination'] as String,
      );

  bool get isSms => channel.toLowerCase() == 'sms';
  String get channelLabel => isSms ? 'SMS' : 'Email';
}
