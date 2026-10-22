using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NguyenThanhLoi_2380601289.ChamCong.Api.Options;

namespace NguyenThanhLoi_2380601289.ChamCong.Api.Services.Messaging;

public class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.Username) || string.IsNullOrWhiteSpace(o.AppPassword))
            throw AppException.BadGateway("Chua cau hinh Email (Username/AppPassword)");

        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(string.IsNullOrWhiteSpace(o.From) ? o.Username : o.From));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new TextPart("plain") { Text = body };

        try
        {
            using var client = new SmtpClient { Timeout = 15_000 };
            await client.ConnectAsync(o.SmtpHost, o.SmtpPort, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(o.Username, o.AppPassword, ct);
            await client.SendAsync(msg, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Gui email that bai toi {To}", to);
            throw AppException.BadGateway("Khong gui duoc email: " + ex.Message);
        }
    }
}
