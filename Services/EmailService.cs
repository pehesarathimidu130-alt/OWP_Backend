using System.Net;
using System.Net.Mail;

namespace Backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetCode)
        {
            var host = _configuration["SmtpSettings:Host"] ?? "smtp.gmail.com";
            var portStr = _configuration["SmtpSettings:Port"] ?? "587";
            var enableSsl = bool.Parse(_configuration["SmtpSettings:EnableSsl"] ?? "true");
            var senderEmail = _configuration["SmtpSettings:SenderEmail"] ?? "";
            var senderName = _configuration["SmtpSettings:SenderName"] ?? "Oleena Wedding Planner";
            var username = _configuration["SmtpSettings:Username"] ?? "";
            var password = _configuration["SmtpSettings:Password"] ?? "";

            int.TryParse(portStr, out int port);
            if (port <= 0) port = 587;

            // Always log the code to console for development verification
            _logger.LogInformation("=================================================");
            _logger.LogInformation("PASSWORD RESET VERIFICATION CODE FOR {Email}: {Code}", toEmail, resetCode);
            _logger.LogInformation("=================================================");

            // If credentials are not configured yet, skip sending and warn
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("SMTP credentials not configured in SmtpSettings. Email to {Email} was skipped. Verification code is logged above.", toEmail);
                return;
            }

            try
            {
                var fromAddress = new MailAddress(string.IsNullOrWhiteSpace(senderEmail) ? username : senderEmail, senderName);
                var toAddress = new MailAddress(toEmail);

                using var mailMessage = new MailMessage(fromAddress, toAddress)
                {
                    Subject = "Oleena Wedding Planner - Password Reset Code",
                    Body = $@"
<!DOCTYPE html>
<html>
<head>
  <style>
    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #F9FAFB; margin: 0; padding: 20px; }}
    .container {{ max-width: 520px; margin: 0 auto; background: #ffffff; border-radius: 16px; padding: 32px; box-shadow: 0 4px 14px rgba(0,0,0,0.05); }}
    .header {{ text-align: center; margin-bottom: 24px; }}
    .brand {{ color: #8E406F; font-size: 24px; font-weight: 700; margin: 0; }}
    .title {{ font-size: 20px; color: #1F2937; margin-top: 12px; margin-bottom: 8px; }}
    .message {{ font-size: 14px; color: #4B5563; line-height: 1.6; margin-bottom: 24px; }}
    .code-box {{ text-align: center; background-color: #FDF0F4; border: 1px dashed #8E406F; border-radius: 12px; padding: 18px; margin: 24px 0; }}
    .code {{ font-size: 32px; font-weight: 700; letter-spacing: 6px; color: #8E406F; margin: 0; }}
    .footer {{ font-size: 12px; color: #9CA3AF; text-align: center; margin-top: 32px; border-top: 1px solid #F3F4F6; padding-top: 16px; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1 class='brand'>OLEENA</h1>
      <h2 class='title'>Password Reset Request</h2>
    </div>
    <p class='message'>Hello,</p>
    <p class='message'>We received a request to reset the password for your Oleena Wedding Planner account. Use the verification code below to complete the reset process:</p>
    <div class='code-box'>
      <div class='code'>{resetCode}</div>
    </div>
    <p class='message'>This code will expire in <strong>15 minutes</strong>. If you did not make this request, please safely ignore this email.</p>
    <div class='footer'>
      &copy; {DateTime.UtcNow.Year} Oleena Wedding Planner. All rights reserved.
    </div>
  </div>
</body>
</html>",
                    IsBodyHtml = true
                };

                using var smtpClient = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl,
                    Timeout = 15000
                };

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Password reset email successfully dispatched to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}. Code was: {Code}", toEmail, resetCode);
                // Do not rethrow so user isn't blocked if SMTP fails
            }
        }
    }
}
