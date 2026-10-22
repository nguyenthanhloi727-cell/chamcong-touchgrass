namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services;

/// <summary>Lỗi nghiệp vụ có mã HTTP; middleware trả { "error": "..." }.</summary>
public class AppException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static AppException BadRequest(string m) => new(400, m);
    public static AppException Unauthorized(string m) => new(401, m);
    public static AppException Forbidden(string m) => new(403, m);
    public static AppException NotFound(string m) => new(404, m);
    public static AppException Conflict(string m) => new(409, m);
    public static AppException TooManyRequests(string m) => new(429, m);
    public static AppException BadGateway(string m) => new(502, m);
}
