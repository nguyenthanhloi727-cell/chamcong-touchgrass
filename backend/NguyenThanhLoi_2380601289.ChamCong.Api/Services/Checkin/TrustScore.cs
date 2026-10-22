namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Checkin;

public record TrustBreakdown(int Gps, int Qr, int NotMocked);

/// <summary>Điểm tin cậy 0-100: GPS 45 + QR 35 + không GPS giả 20.</summary>
public static class TrustScore
{
    public const int GpsPoints = 45, QrPoints = 35, NotMockedPoints = 20;
    public const int PassThreshold = 70;

    public static (int Total, TrustBreakdown Breakdown) Compute(bool gpsOk, bool qrValid, bool isMocked)
    {
        var b = new TrustBreakdown(
            gpsOk && !isMocked ? GpsPoints : 0, // GPS giả -> điểm GPS = 0
            qrValid ? QrPoints : 0,
            isMocked ? 0 : NotMockedPoints);
        return (b.Gps + b.Qr + b.NotMocked, b);
    }

    public static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000;
        static double Rad(double d) => d * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLng = Rad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }
}
