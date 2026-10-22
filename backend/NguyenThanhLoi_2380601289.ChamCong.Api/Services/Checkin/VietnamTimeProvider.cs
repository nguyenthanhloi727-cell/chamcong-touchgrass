namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Checkin;

/// <summary>Giờ chấm công luôn theo giờ Việt Nam (UTC+7), không phụ thuộc múi giờ của máy chạy backend.</summary>
public sealed class VietnamTimeProvider : TimeProvider
{
    private static readonly TimeZoneInfo Vn = Find();

    public override TimeZoneInfo LocalTimeZone => Vn;

    private static TimeZoneInfo Find()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("VN", TimeSpan.FromHours(7), "Viet Nam", "Viet Nam");
    }
}
