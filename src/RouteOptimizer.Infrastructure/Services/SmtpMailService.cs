using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Domain.Common;
using RouteOptimizer.Infrastructure.Settings;

namespace RouteOptimizer.Infrastructure.Services;

public sealed class SmtpMailService : IMailService
{
    private readonly SmtpSettings _settings;

    public SmtpMailService(IOptions<SmtpSettings> options) => _settings = options.Value;

    public async Task<Result> SendAsync(MailMessage message, CancellationToken ct)
    {
        var msg = new MimeMessage();

        msg.From.Add(new MailboxAddress("Route Optimizer", _settings.From));
        msg.To.Add(new MailboxAddress(message.To, message.To));

        msg.Subject = message.Subject;

        var body = new BodyBuilder();

        if (message.IsHtml)
            body.HtmlBody = message.Body;

        else
            body.TextBody = message.Body;

        msg.Body = body.ToMessageBody();

        using var client = new MailKit.Net.Smtp.SmtpClient();

        var ssl = _settings.UseSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await client.ConnectAsync(_settings.Host, _settings.Port, ssl, ct);

        if (_settings.Username is not null)
            await client.AuthenticateAsync(_settings.Username, _settings.Password ?? string.Empty, ct);

        await client.SendAsync(msg, ct);
        await client.DisconnectAsync(true, ct);

        return Result.Success();
    }
}
