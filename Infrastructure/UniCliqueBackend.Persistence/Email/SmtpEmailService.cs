using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
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
                    if (env.Equals("Development", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("DEV EMAIL (no SMTP configured):");
                        Console.WriteLine($"To: {to}");
                        Console.WriteLine($"Subject: {subject}");
                        Console.WriteLine($"Body: {body}");
                        return;
                    }
                    throw new InvalidOperationException("Email SMTP configuration is missing.");
                }
            }

            var port = int.Parse(portStr!);
            var enableSsl = true;
            if (bool.TryParse(enableSslStr, out var parsed))
            {
                enableSsl = parsed;
            }
            var timeoutMs = 60000; // Increased to 60s for cloud environments
            if (int.TryParse(timeoutStr, out var parsedTimeout) && parsedTimeout > 0)
            {
                timeoutMs = parsedTimeout;
            }

            Console.WriteLine($"[SMTP-DEBUG] Attempting SMTP via {host}:{port} (SSL: {enableSsl}, Timeout: {timeoutMs}ms)");

            using var smtp = new SmtpClient(host!, port);
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(username, password);
            smtp.EnableSsl = enableSsl;
            smtp.Timeout = timeoutMs;

            MailAddress fromAddr;
            MailAddress toAddr;
            try
            {
                fromAddr = new MailAddress(from!);
                toAddr = new MailAddress(to);
            }
            catch
            {
                Console.WriteLine("[SMTP-DEBUG] Failed: invalid email format for From/To");
                throw new SmtpException("SMTP invalid email format.");
            }

            using var mail = new MailMessage(fromAddr, toAddr)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            if (!string.IsNullOrWhiteSpace(replyTo))
            {
                mail.ReplyToList.Add(new MailAddress(replyTo));
            }
            try
            {
                var sendTask = smtp.SendMailAsync(mail);
                var delayTask = Task.Delay(timeoutMs);
                await Task.WhenAny(sendTask, delayTask);
                if (!sendTask.IsCompleted)
                {
                    Console.WriteLine($"[SMTP-DEBUG] Failed: SMTP send timeout after {timeoutMs}ms");
                    throw new TimeoutException("SMTP send timeout");
                }
                await sendTask;
                Console.WriteLine("[SMTP-DEBUG] Success: Email sent successfully.");
            }
            catch (SmtpException ex)
            {
                var msg = ex.Message ?? "";
                Console.WriteLine($"[SMTP-DEBUG] SMTP exception: {ex.StatusCode} {msg}");
                if (msg.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("Username and Password not accepted", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("5.7", StringComparison.OrdinalIgnoreCase))
                {
                    throw new SmtpException(SmtpStatusCode.GeneralFailure, $"SMTP auth failed: {msg}");
                }
                throw new SmtpException(SmtpStatusCode.GeneralFailure, $"SMTP connect failed: {msg}");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[SMTP-DEBUG] Socket exception: {ex.SocketErrorCode} {ex.Message}");
                throw new SmtpException(SmtpStatusCode.GeneralFailure, "SMTP network unreachable.");
            }
        }
    }
}
