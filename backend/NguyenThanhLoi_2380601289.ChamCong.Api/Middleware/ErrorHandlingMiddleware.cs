using Microsoft.AspNetCore.Http;
using NguyenThanhLoi_2380601289.ChamCong.Api.Services;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Middleware;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
            // 401/403 do JwtBearer/authorization sinh ra (không có body) → thêm body { error }
            if (!ctx.Response.HasStarted && ctx.Response.ContentLength is null && string.IsNullOrEmpty(ctx.Response.ContentType))
            {
                var msg = ctx.Response.StatusCode switch
                {
                    401 => "Chua dang nhap hoac token khong hop le",
                    403 => "Khong co quyen thuc hien",
                    404 => "Khong tim thay",
                    _ => null
                };
                if (msg is not null) await ctx.Response.WriteAsJsonAsync(new { error = msg });
            }
        }
        catch (AppException ex)
        {
            await Write(ctx, ex.StatusCode, ex.Message);
        }
        catch (BadHttpRequestException ex)
        {
            await Write(ctx, 400, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Loi khong xu ly duoc: {Path}", ctx.Request.Path);
            await Write(ctx, 500, "Loi he thong");
        }
    }

    private static async Task Write(HttpContext ctx, int status, string message)
    {
        if (ctx.Response.HasStarted) return;
        ctx.Response.Clear();
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new { error = message });
    }
}
