using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using UniCliqueBackend.Application.Interfaces.Services;

namespace UniCliqueBackend.Persistence.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var host = _configuration["Email:SmtpHost"];
            var portStr = _configuration["Email:SmtpPort"];
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            var from = _configuration["Email:From"];
            var provider = _configuration["Email:Provider"];
            var enableSslStr = _configuration["Email:EnableSsl"];
            var replyTo = _configuration["Email:ReplyTo"];
            var timeoutStr = _configuration["Email:TimeoutMs"];

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var missing = string.IsNullOrWhiteSpace(host) ||
                          string.IsNullOrWhiteSpace(portStr) ||
                          string.IsNullOrWhiteSpace(username) ||
                          string.IsNullOrWhiteSpace(password) ||
                          string.IsNullOrWhiteSpace(from);

            if (missing)
            {
                if (!string.IsNullOrWhiteSpace(provider))
                {
                    switch (provider.ToLowerInvariant())
                    {
                        case "gmail":
                            host = "smtp.gmail.com";
                            portStr = string.IsNullOrWhiteSpace(portStr) ? "587" : portStr;
                            break;
                        case "outlook":
                        case "hotmail":
                            host = "smtp-mail.outlook.com";
                            portStr = string.IsNullOrWhiteSpace(portStr) ? "587" : portStr;
                            break;
                        case "office365":
                            host = "smtp.office365.com";
                            portStr = string.IsNullOrWhiteSpace(portStr) ? "587" : portStr;
                            break;
                    }
                }
                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(portStr) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(from))
                {
                    throw new InvalidOperationException("Email SMTP configuration is missing.");
                }
            }

            var port = int.Parse(portStr!);
            var enableSsl = true;
            if (bool.TryParse(enableSslStr, out var parsed))
            {
                enableSsl = parsed;
            }
            var timeoutMs = 15000;
            if (int.TryParse(timeoutStr, out var parsedTimeout) && parsedTimeout > 0)
            {
                timeoutMs = parsedTimeout;
            }

            using var smtp = new SmtpClient(host!, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl,
                Timeout = timeoutMs
            };

            using var mail = new MailMessage(from!, to, subject, body);
            if (!string.IsNullOrWhiteSpace(replyTo))
            {
                mail.ReplyToList.Add(new MailAddress(replyTo));
            }
            var sendTask = smtp.SendMailAsync(mail);
            var delayTask = Task.Delay(timeoutMs);
            var completed = await Task.WhenAny(sendTask, delayTask);
            if (completed != sendTask)
            {
                try { smtp.Dispose(); } catch { }
                throw new TimeoutException("SMTP send timeout");
            }
            await sendTask;
        }
    }
}
