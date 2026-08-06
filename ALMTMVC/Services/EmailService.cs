using ALMTMVC.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ALMTMVC.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> settings,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendCompanyNotificationAsync(
        ContactEnquiry enquiry)
    {
        string subject =
            $"New website enquiry: {enquiry.ServiceRequired}";

        string body = $"""
            <h2>New Almighty Lift Consultants Enquiry</h2>

            <p>
                A new customer enquiry has been submitted through
                the website.
            </p>

            <table style="border-collapse: collapse; width: 100%;">
                <tr>
                    <td style="padding: 8px; font-weight: bold;">Name</td>
                    <td style="padding: 8px;">
                        {Encode(enquiry.FullName)}
                    </td>
                </tr>

                <tr>
                    <td style="padding: 8px; font-weight: bold;">Email</td>
                    <td style="padding: 8px;">
                        {Encode(enquiry.Email)}
                    </td>
                </tr>

                <tr>
                    <td style="padding: 8px; font-weight: bold;">Phone</td>
                    <td style="padding: 8px;">
                        {Encode(enquiry.PhoneNumber ?? "Not provided")}
                    </td>
                </tr>

                <tr>
                    <td style="padding: 8px; font-weight: bold;">
                        Company or building
                    </td>
                    <td style="padding: 8px;">
                        {Encode(enquiry.CompanyName ?? "Not provided")}
                    </td>
                </tr>

                <tr>
                    <td style="padding: 8px; font-weight: bold;">Service</td>
                    <td style="padding: 8px;">
                        {Encode(enquiry.ServiceRequired)}
                    </td>
                </tr>

                <tr>
                    <td style="padding: 8px; font-weight: bold;">
                        Location
                    </td>
                    <td style="padding: 8px;">
                        {Encode(enquiry.ProjectLocation ?? "Not provided")}
                    </td>
                </tr>

                <tr>
                    <td style="padding: 8px; font-weight: bold;">
                        Message
                    </td>
                    <td style="padding: 8px;">
                        {Encode(enquiry.Message)}
                    </td>
                </tr>

                <tr>
                    <td style="padding: 8px; font-weight: bold;">
                        Submitted
                    </td>
                    <td style="padding: 8px;">
                {enquiry.SubmittedAtUtc.ToLocalTime():dd MMM yyyy HH:mm}
            </td>
                </tr>
            </table>
            """;

        await SendEmailAsync(
            _settings.CompanyNotificationEmail,
            subject,
            body,
            enquiry.Email);
    }

    public async Task SendCustomerConfirmationAsync(
        ContactEnquiry enquiry)
    {
        string subject =
            "We received your enquiry | Almighty Lift Consultants";

        string body = $"""
            <h2>Thank you for contacting Almighty Lift Consultants</h2>

            <p>Dear {Encode(enquiry.FullName)},</p>

            <p>
                We have received your enquiry regarding
                <strong>{Encode(enquiry.ServiceRequired)}</strong>.
            </p>

            <p>
                Our team will review the information provided and
                contact you using the details submitted.
            </p>

            <p>
                <strong>Your message:</strong>
            </p>

            <p>{Encode(enquiry.Message)}</p>

            <p>
                Kind regards,<br />
                <strong>Almighty Lift Consultants</strong>
            </p>
            """;

        await SendEmailAsync(
            enquiry.Email,
            subject,
            body);
    }

    public async Task SendAdminReplyAsync(
        ContactEnquiry enquiry,
        string subject,
        string message)
    {
        string safeSubject = subject.Trim();
        string safeMessage = Encode(message)
            .Replace("\r\n", "<br />")
            .Replace("\n", "<br />");

        string body = $"""
            <h2>Almighty Lift Consultants</h2>

            <p>Dear {Encode(enquiry.FullName)},</p>

            <p>
                {safeMessage}
            </p>

            <hr style="margin: 28px 0; border: 0;
                       border-top: 1px solid #e5e7eb;" />

            <p style="color: #666666; font-size: 14px;">
                This reply relates to your enquiry regarding
                <strong>{Encode(enquiry.ServiceRequired)}</strong>.
            </p>

            <p>
                Kind regards,<br />
                <strong>Almighty Lift Consultants</strong>
            </p>
            """;

        await SendEmailAsync(
            enquiry.Email,
            safeSubject,
            body,
            _settings.CompanyNotificationEmail);
    }

    private async Task SendEmailAsync(
        string recipient,
        string subject,
        string htmlBody,
        string? replyTo = null)
    {
        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _settings.SenderName,
                _settings.SenderEmail));

        message.To.Add(
            MailboxAddress.Parse(recipient));

        if (!string.IsNullOrWhiteSpace(replyTo))
        {
            message.ReplyTo.Add(
                MailboxAddress.Parse(replyTo));
        }

        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody
        }.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                _settings.SmtpServer,
                _settings.SmtpPort,
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                _settings.Username,
                _settings.Password);

            await client.SendAsync(message);

            await client.DisconnectAsync(true);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Email sending failed for recipient {Recipient}.",
                recipient);

            throw;
        }
    }

    private static string Encode(string value)
    {
        return System.Net.WebUtility.HtmlEncode(value);
    }
}