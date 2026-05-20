using RouteOptimizer.Domain.Common;

namespace RouteOptimizer.Application.Abstractions;

public interface IMailService
{
    Task <Result> SendAsync(MailMessage message, CancellationToken ct = default);
}

public sealed record MailMessage(string To, string Subject, string Body, bool IsHtml = false);
