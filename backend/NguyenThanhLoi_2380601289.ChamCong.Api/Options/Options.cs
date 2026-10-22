namespace NguyenThanhLoi_2380601289.ChamCong.Api.Options;

public class JwtOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string Key { get; set; } = "";
    public int AccessMinutes { get; set; } = 15;
    public int RefreshDays { get; set; } = 7;
}

public class OtpOptions
{
    public int ExpireMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public int ResendSeconds { get; set; } = 60;
    public string HashPepper { get; set; } = "";
}

public class EmailOptions
{
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = "";
    public string AppPassword { get; set; } = "";
    public string From { get; set; } = "";
}

public class SmsGatewayOptions
{
    public string BaseUrl { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int SimNumber { get; set; } = 1;
    public int TimeoutSeconds { get; set; } = 10;
    public int PollIntervalSeconds { get; set; } = 2;
    public int PollMaxSeconds { get; set; } = 15;
}

public class GoogleOptions
{
    public string WebClientId { get; set; } = "";
}

public class QrOptions
{
    public string Secret { get; set; } = "";
    public int WindowSeconds { get; set; } = 30;
}

public class FaceOptions
{
    /// <summary>Ngưỡng cosine similarity để coi là khớp (chưa chốt, chỉnh khi test thật).</summary>
    public double Threshold { get; set; } = 0.8;
    public int FaceTokenSeconds { get; set; } = 120;
}

public class GeofenceOptions
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public int RadiusMeters { get; set; } = 100;
}
