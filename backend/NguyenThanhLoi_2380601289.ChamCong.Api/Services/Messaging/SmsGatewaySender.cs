using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NguyenThanhLoi_2380601289.ChamCong.Api.Options;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Messaging;

/// <summary>SMS Gateway for Android (Local Server): POST /messages → 202 + id, rồi poll GET /messages/{id}.</summary>
public class SmsGatewaySender(HttpClient http, IOptions<SmsGatewayOptions> options, ILogger<SmsGatewaySender> logger) : ISmsSender
{
    public async Task<SmsResult> SendAsync(string phone, string text, CancellationToken ct)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.BaseUrl))
            return new SmsResult(false, null, null, "Chua cau hinh SmsGateway:BaseUrl");

        var baseUrl = o.BaseUrl.TrimEnd('/');
        var auth = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{o.Username}:{o.Password}")));
        string? id = null;
        try
        {
            using var post = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/messages")
            {
                Content = JsonContent.Create(new
                {
                    textMessage = new { text },
                    phoneNumbers = new[] { phone },
                    simNumber = o.SimNumber,
                }),
            };
            post.Headers.Authorization = auth;
            using var res = await http.SendAsync(post, ct);
            if (res.StatusCode != System.Net.HttpStatusCode.Accepted)
                return new SmsResult(false, null, null, $"Gateway tra ve HTTP {(int)res.StatusCode}");

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            id = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrEmpty(id))
                return new SmsResult(false, null, null, "Gateway khong tra ve id tin nhan");

            // Poll trạng thái: chỉ Sent/Delivered mới là thành công
            var deadline = DateTime.UtcNow.AddSeconds(o.PollMaxSeconds);
            var state = "Pending";
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromSeconds(o.PollIntervalSeconds), ct);

                using var get = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/messages/{id}");
                get.Headers.Authorization = auth;
                using var st = await http.SendAsync(get, ct);
                if (!st.IsSuccessStatusCode)
                    return new SmsResult(false, id, state, $"Gateway tra ve HTTP {(int)st.StatusCode} khi tra trang thai");

                using var sd = JsonDocument.Parse(await st.Content.ReadAsStringAsync(ct));
                state = sd.RootElement.TryGetProperty("state", out var s) ? s.GetString() ?? state : state;
                logger.LogInformation("SMS {Id} state={State}", id, state);

                if (state is "Sent" or "Delivered")
                    return new SmsResult(true, id, state, null);
                if (state == "Failed")
                    return new SmsResult(false, id, state, ReadError(sd.RootElement) ?? "Gateway bao Failed");
            }
            return new SmsResult(false, id, state, "Het thoi gian cho gateway gui SMS (timeout)");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            if (ct.IsCancellationRequested) throw;
            logger.LogWarning(ex, "Goi SMS gateway that bai");
            return new SmsResult(false, id, null, "Khong ket noi duoc SMS gateway: " + ex.Message);
        }
    }

    private static string? ReadError(JsonElement root)
    {
        if (!root.TryGetProperty("recipients", out var rs) || rs.ValueKind != JsonValueKind.Array) return null;
        foreach (var r in rs.EnumerateArray())
            if (r.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String)
                return e.GetString();
        return null;
    }
}
