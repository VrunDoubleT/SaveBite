using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SaveBite.Backend.Configurations;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        var email = new MimeMessage();

        email.From.Add(
            new MailboxAddress(
                _options.FromName,
                _options.FromEmail));

        email.To.Add(
            MailboxAddress.Parse(to));

        email.Subject = subject;

        email.Body = new BodyBuilder
        {
            HtmlBody = isHtml ? body : null,
            TextBody = isHtml ? null : body
        }.ToMessageBody();

        using var smtp = new SmtpClient();

        var secureSocketOptions = _options.UseSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        try
        {
            await smtp.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, cancellationToken);
            await smtp.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new EmailSendException("SMTP connection/authentication failed before sending.", false, ex);
        }
        try { await smtp.SendAsync(email, cancellationToken); }
        catch (SmtpCommandException ex)
        {
            throw new EmailSendException("SMTP rejected the message.", false, ex);
        }
        catch (Exception ex)
        {
            throw new EmailSendException("SMTP did not confirm whether the message was accepted.", true, ex);
        }
        // An accepted message remains successful even if the subsequent QUIT fails.
        try { await smtp.DisconnectAsync(true, cancellationToken); }
        catch (Exception ex) { _logger.LogWarning(ex, "SMTP disconnect failed after accepting the email."); }

        _logger.LogInformation(
            "Email sent successfully to {Email}. Subject: {Subject}",
            to,
            subject);
    }
}