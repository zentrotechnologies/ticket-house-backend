using Microsoft.Extensions.Logging;
using MODEL.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace BAL.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
        Task<bool> SendOTPEmailAsync(string toEmail, string otp, string userName = "");
    }
    public class EmailService: IEmailService
    {
        private readonly THConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(THConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient(_configuration.SmtpServer, _configuration.SmtpPort))
                {
                    client.EnableSsl = _configuration.EnableSsl;
                    client.Credentials = new NetworkCredential(_configuration.SmtpUsername, _configuration.SmtpPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_configuration.FromEmail, _configuration.FromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Email sent successfully to {toEmail}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                return false;
            }
        }

        public async Task<bool> SendOTPEmailAsync(string toEmail, string otp, string userName = "")
        {
            var subject = "Your OTP for TicketHouse";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .otp-code {{ font-size: 32px; font-weight: bold; text-align: center; margin: 30px 0; color: #667eea; }}
                        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; text-align: center; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>TicketHouse</h1>
                        </div>
                        <h2>Hello {(string.IsNullOrEmpty(userName) ? "User" : userName)}!</h2>
                        <p>Your One-Time Password (OTP) for verification is:</p>
                        <div class='otp-code'>{otp}</div>
                        <p>This OTP is valid for 2 minutes. Please do not share this code with anyone.</p>
                        <p>If you didn't request this OTP, please ignore this email.</p>
                        <div class='footer'>
                            <p>Thank you for choosing TicketHouse!</p>
                            <p><strong>TicketHouse Team</strong></p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }
    }
}
