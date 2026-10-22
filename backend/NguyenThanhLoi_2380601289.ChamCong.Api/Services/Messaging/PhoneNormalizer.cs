using System.Text.RegularExpressions;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Messaging;

public static partial class PhoneNormalizer
{
    [GeneratedRegex(@"^0[35789]\d{8}$")]
    private static partial Regex LocalRegex();

    [GeneratedRegex(@"^\+84[35789]\d{8}$")]
    private static partial Regex E164Regex();

    /// <summary>0xxxxxxxxx → +84xxxxxxxxx; chấp nhận sẵn +84...; sai định dạng → 400.</summary>
    public static string Normalize(string? phone)
    {
        var p = new string((phone ?? "").Where(c => !char.IsWhiteSpace(c) && c is not '.' and not '-').ToArray());
        if (E164Regex().IsMatch(p)) return p;
        if (LocalRegex().IsMatch(p)) return "+84" + p[1..];
        throw AppException.BadRequest("So dien thoai khong hop le (dang 0xxxxxxxxx hoac +84xxxxxxxxx)");
    }
}
