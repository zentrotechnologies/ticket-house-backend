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
        Task<bool> SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachment, string attachmentFileName);
        Task<bool> SendOTPEmailAsync(string toEmail, string otp, string userName = "");
        Task<bool> TestSmtpConnectionAsync();
        Task<bool> SendForgotPasswordOTPEmailAsync(string email, string otp, string userName);
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

        //public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        //{
        //    try
        //    {
        //        // Log all configuration details
        //        _logger.LogInformation("=== EMAIL SERVICE CONFIGURATION ===");
        //        _logger.LogInformation($"SMTP Server: {_configuration.SmtpServer}:{_configuration.SmtpPort}");
        //        _logger.LogInformation($"Username: {_configuration.SmtpUsername}");
        //        _logger.LogInformation($"From Email: {_configuration.FromEmail}");
        //        _logger.LogInformation($"Enable SSL: {_configuration.EnableSsl}");
        //        _logger.LogInformation($"Password Length: {_configuration.SmtpPassword?.Length ?? 0}");
        //        _logger.LogInformation($"To: {toEmail}");
        //        _logger.LogInformation($"Subject: {subject}");
        //        _logger.LogInformation("=================================");

        //        using (var client = new SmtpClient(_configuration.SmtpServer, _configuration.SmtpPort))
        //        {
        //            _logger.LogInformation($"Attempting to send email to: {toEmail}");
        //            _logger.LogInformation($"SMTP Server: {_configuration.SmtpServer}:{_configuration.SmtpPort}");
        //            _logger.LogInformation($"SMTP Username: {_configuration.SmtpUsername}");
        //            _logger.LogInformation($"From Email: {_configuration.FromEmail}");

        //            client.EnableSsl = _configuration.EnableSsl;
        //            client.Credentials = new NetworkCredential(_configuration.SmtpUsername, _configuration.SmtpPassword);
        //            client.Timeout = 30000; // 30 seconds

        //            var mailMessage = new MailMessage
        //            {
        //                From = new MailAddress(_configuration.FromEmail, _configuration.FromName),
        //                Subject = subject,
        //                Body = body,
        //                IsBodyHtml = true
        //            };
        //            mailMessage.To.Add(toEmail);

        //            await client.SendMailAsync(mailMessage);
        //            _logger.LogInformation($"Email sent successfully to {toEmail}");
        //            return true;
        //        }
        //    }
        //    catch (SmtpException smtpEx)
        //    {
        //        _logger.LogError(smtpEx, $"✗ SMTP Error sending to {toEmail}");
        //        _logger.LogError($"Status Code: {smtpEx.StatusCode}");
        //        _logger.LogError($"Message: {smtpEx.Message}");

        //        if (smtpEx.InnerException != null)
        //        {
        //            _logger.LogError($"Inner Exception: {smtpEx.InnerException.Message}");
        //        }

        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Failed to send email to {toEmail}");
        //        return false;
        //    }
        //}


        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            return await SendEmailInternalAsync(toEmail, subject, body, null, null);
        }

        public async Task<bool> SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachment, string attachmentFileName)
        {
            return await SendEmailInternalAsync(toEmail, subject, body, attachment, attachmentFileName);
        }

        //private async Task<bool> SendEmailInternalAsync(string toEmail, string subject, string body, byte[] attachment, string attachmentFileName)
        //{
        //    try
        //    {
        //        _logger.LogInformation("=== EMAIL SERVICE CONFIGURATION ===");
        //        _logger.LogInformation($"SMTP Server: {_configuration.SmtpServer}:{_configuration.SmtpPort}");
        //        _logger.LogInformation($"Username: {_configuration.SmtpUsername}");
        //        _logger.LogInformation($"From Email: {_configuration.FromEmail}");
        //        _logger.LogInformation($"Enable SSL: {_configuration.EnableSsl}");
        //        _logger.LogInformation($"To: {toEmail}");
        //        _logger.LogInformation($"Subject: {subject}");
        //        _logger.LogInformation($"Has Attachment: {attachment != null}");
        //        _logger.LogInformation("=================================");

        //        using (var client = new SmtpClient(_configuration.SmtpServer, _configuration.SmtpPort))
        //        {
        //            client.EnableSsl = _configuration.EnableSsl;
        //            client.Credentials = new NetworkCredential(_configuration.SmtpUsername, _configuration.SmtpPassword);
        //            client.Timeout = 30000;
        //            client.DeliveryMethod = SmtpDeliveryMethod.Network;
        //            client.UseDefaultCredentials = false;

        //            var mailMessage = new MailMessage
        //            {
        //                From = new MailAddress(_configuration.FromEmail, _configuration.FromName ?? "TicketHouse"),
        //                Subject = subject,
        //                Body = body,
        //                IsBodyHtml = true,
        //                Priority = MailPriority.Normal
        //            };

        //            // Add important headers to avoid SPAM
        //            mailMessage.Headers.Add("List-Unsubscribe", $"<mailto:unsubscribe@tickethouse.in?subject=unsubscribe>");
        //            mailMessage.Headers.Add("Precedence", "bulk");
        //            mailMessage.Headers.Add("X-Auto-Response-Suppress", "OOF, AutoReply");
        //            mailMessage.Headers.Add("X-Mailer", "TicketHouse Mail System");
        //            mailMessage.Headers.Add("X-Priority", "3");

        //            // Add Message-ID header
        //            mailMessage.Headers.Add("Message-ID", $"<{Guid.NewGuid().ToString()}@tickethouse.in>");

        //            mailMessage.To.Add(toEmail);
        //            mailMessage.ReplyToList.Add(new MailAddress(_configuration.FromEmail, "TicketHouse Support"));

        //            // Add plain text version for better spam score
        //            var plainTextView = AlternateView.CreateAlternateViewFromString(
        //                StripHtml(body),
        //                null,
        //                "text/plain"
        //            );
        //            mailMessage.AlternateViews.Add(plainTextView);

        //            if (attachment != null && !string.IsNullOrEmpty(attachmentFileName))
        //            {
        //                using (var stream = new MemoryStream(attachment))
        //                {
        //                    var attachment_file = new Attachment(stream, attachmentFileName, "image/png");
        //                    mailMessage.Attachments.Add(attachment_file);
        //                    _logger.LogInformation($"Added attachment: {attachmentFileName}, Size: {attachment.Length} bytes");
        //                    await client.SendMailAsync(mailMessage);
        //                }
        //            }
        //            else
        //            {
        //                await client.SendMailAsync(mailMessage);
        //            }

        //            _logger.LogInformation($"Email sent successfully to {toEmail}");
        //            return true;
        //        }
        //    }
        //    catch (SmtpException smtpEx)
        //    {
        //        _logger.LogError(smtpEx, $"✗ SMTP Error sending to {toEmail}");
        //        _logger.LogError($"Status Code: {smtpEx.StatusCode}");
        //        _logger.LogError($"Message: {smtpEx.Message}");

        //        if (smtpEx.InnerException != null)
        //        {
        //            _logger.LogError($"Inner Exception: {smtpEx.InnerException.Message}");
        //        }
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Failed to send email to {toEmail}");
        //        return false;
        //    }
        //}

        //private async Task<bool> SendEmailInternalAsync(string toEmail, string subject, string body, byte[] attachment, string attachmentFileName)
        //{
        //    try
        //    {
        //        _logger.LogInformation("=== EMAIL SERVICE CONFIGURATION ===");
        //        _logger.LogInformation($"SMTP Server: {_configuration.SmtpServer}:{_configuration.SmtpPort}");
        //        _logger.LogInformation($"Username: {_configuration.SmtpUsername}");
        //        _logger.LogInformation($"From Email: {_configuration.FromEmail}");
        //        _logger.LogInformation($"Enable SSL: {_configuration.EnableSsl}");
        //        _logger.LogInformation($"To: {toEmail}");
        //        _logger.LogInformation($"Subject: {subject}");
        //        _logger.LogInformation($"Has Attachment: {attachment != null}");
        //        _logger.LogInformation("=================================");

        //        using (var client = new SmtpClient(_configuration.SmtpServer, _configuration.SmtpPort))
        //        {
        //            client.EnableSsl = _configuration.EnableSsl;
        //            client.Credentials = new NetworkCredential(_configuration.SmtpUsername, _configuration.SmtpPassword);
        //            client.Timeout = 30000;
        //            client.DeliveryMethod = SmtpDeliveryMethod.Network;
        //            client.UseDefaultCredentials = false;

        //            var mailMessage = new MailMessage
        //            {
        //                From = new MailAddress(_configuration.FromEmail, _configuration.FromName ?? "TicketHouse"),
        //                Subject = subject,
        //                Body = body,
        //                IsBodyHtml = true,
        //                BodyEncoding = System.Text.Encoding.UTF8,  // Added explicit UTF-8 encoding
        //                Priority = MailPriority.Normal
        //            };

        //            // Add important headers to avoid SPAM
        //            mailMessage.Headers.Add("List-Unsubscribe", $"<mailto:unsubscribe@tickethouse.in?subject=unsubscribe>");
        //            mailMessage.Headers.Add("Precedence", "bulk");
        //            mailMessage.Headers.Add("X-Auto-Response-Suppress", "OOF, AutoReply");
        //            mailMessage.Headers.Add("X-Mailer", "TicketHouse Mail System");
        //            mailMessage.Headers.Add("X-Priority", "3");
        //            mailMessage.Headers.Add("Content-Type", "text/html; charset=utf-8"); // Add this header too

        //            // Add Message-ID header
        //            mailMessage.Headers.Add("Message-ID", $"<{Guid.NewGuid().ToString()}@tickethouse.in>");

        //            mailMessage.To.Add(toEmail);
        //            mailMessage.ReplyToList.Add(new MailAddress(_configuration.FromEmail, "TicketHouse Support"));

        //            // Add plain text version for better spam score
        //            var plainTextView = AlternateView.CreateAlternateViewFromString(
        //                StripHtml(body),
        //                null,
        //                "text/plain"
        //            );
        //            mailMessage.AlternateViews.Add(plainTextView);

        //            if (attachment != null && !string.IsNullOrEmpty(attachmentFileName))
        //            {
        //                using (var stream = new MemoryStream(attachment))
        //                {
        //                    var attachment_file = new Attachment(stream, attachmentFileName, "image/png");
        //                    mailMessage.Attachments.Add(attachment_file);
        //                    _logger.LogInformation($"Added attachment: {attachmentFileName}, Size: {attachment.Length} bytes");
        //                    await client.SendMailAsync(mailMessage);
        //                }
        //            }
        //            else
        //            {
        //                await client.SendMailAsync(mailMessage);
        //            }

        //            _logger.LogInformation($"Email sent successfully to {toEmail}");
        //            return true;
        //        }
        //    }
        //    catch (SmtpException smtpEx)
        //    {
        //        _logger.LogError(smtpEx, $"✗ SMTP Error sending to {toEmail}");
        //        _logger.LogError($"Status Code: {smtpEx.StatusCode}");
        //        _logger.LogError($"Message: {smtpEx.Message}");

        //        if (smtpEx.InnerException != null)
        //        {
        //            _logger.LogError($"Inner Exception: {smtpEx.InnerException.Message}");
        //        }
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Failed to send email to {toEmail}");
        //        return false;
        //    }
        //}

        //private async Task<bool> SendEmailInternalAsync(string toEmail, string subject, string body, byte[] attachment, string attachmentFileName)
        //{
        //    try
        //    {
        //        _logger.LogInformation("=== EMAIL SERVICE CONFIGURATION ===");
        //        _logger.LogInformation($"SMTP Server: {_configuration.SmtpServer}:{_configuration.SmtpPort}");
        //        _logger.LogInformation($"Username: {_configuration.SmtpUsername}");
        //        _logger.LogInformation($"From Email: {_configuration.FromEmail}");
        //        _logger.LogInformation($"Enable SSL: {_configuration.EnableSsl}");
        //        _logger.LogInformation($"To: {toEmail}");
        //        _logger.LogInformation($"Subject: {subject}");
        //        _logger.LogInformation($"Has Attachment: {attachment != null}");
        //        _logger.LogInformation("=================================");

        //        using (var client = new SmtpClient(_configuration.SmtpServer, _configuration.SmtpPort))
        //        {
        //            client.EnableSsl = _configuration.EnableSsl;
        //            client.Credentials = new NetworkCredential(_configuration.SmtpUsername, _configuration.SmtpPassword);
        //            client.Timeout = 30000;
        //            client.DeliveryMethod = SmtpDeliveryMethod.Network;
        //            client.UseDefaultCredentials = false;

        //            var mailMessage = new MailMessage
        //            {
        //                From = new MailAddress(_configuration.FromEmail, _configuration.FromName ?? "TicketHouse"),
        //                Subject = subject,
        //                Body = body,  // Set HTML content as the main body
        //                IsBodyHtml = true,  // CRITICAL: This tells the email client to render as HTML
        //                BodyEncoding = System.Text.Encoding.UTF8,
        //                Priority = MailPriority.Normal
        //            };

        //            // Add important headers
        //            mailMessage.Headers.Add("List-Unsubscribe", $"<mailto:unsubscribe@tickethouse.in?subject=unsubscribe>");
        //            mailMessage.Headers.Add("Precedence", "bulk");
        //            mailMessage.Headers.Add("X-Auto-Response-Suppress", "OOF, AutoReply");
        //            mailMessage.Headers.Add("X-Mailer", "TicketHouse Mail System");
        //            mailMessage.Headers.Add("X-Priority", "3");
        //            mailMessage.Headers.Add("Message-ID", $"<{Guid.NewGuid().ToString()}@tickethouse.in>");

        //            mailMessage.To.Add(toEmail);
        //            mailMessage.ReplyToList.Add(new MailAddress(_configuration.FromEmail, "TicketHouse Support"));

        //            // Add plain text version as alternate view (for email clients that don't support HTML)
        //            // BUT DO NOT add HTML as alternate view since we already set it as the main body
        //            var plainTextView = AlternateView.CreateAlternateViewFromString(
        //                StripHtml(body),
        //                System.Text.Encoding.UTF8,
        //                "text/plain"
        //            );
        //            mailMessage.AlternateViews.Add(plainTextView);

        //            if (attachment != null && !string.IsNullOrEmpty(attachmentFileName))
        //            {
        //                // Create attachment from byte array
        //                var stream = new MemoryStream(attachment);
        //                var attachment_file = new Attachment(stream, attachmentFileName, "image/png");
        //                mailMessage.Attachments.Add(attachment_file);
        //                _logger.LogInformation($"Added attachment: {attachmentFileName}, Size: {attachment.Length} bytes");
        //            }

        //            // Send the email
        //            await client.SendMailAsync(mailMessage);
        //            _logger.LogInformation($"Email sent successfully to {toEmail}");

        //            // Dispose attachment stream after sending
        //            if (mailMessage.Attachments.Count > 0)
        //            {
        //                foreach (var att in mailMessage.Attachments)
        //                {
        //                    att.Dispose();
        //                }
        //            }

        //            return true;
        //        }
        //    }
        //    catch (SmtpException smtpEx)
        //    {
        //        _logger.LogError(smtpEx, $"✗ SMTP Error sending to {toEmail}");
        //        _logger.LogError($"Status Code: {smtpEx.StatusCode}");
        //        _logger.LogError($"Message: {smtpEx.Message}");

        //        if (smtpEx.InnerException != null)
        //        {
        //            _logger.LogError($"Inner Exception: {smtpEx.InnerException.Message}");
        //        }
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Failed to send email to {toEmail}");
        //        return false;
        //    }
        //}

        private async Task<bool> SendEmailInternalAsync(string toEmail, string subject, string body, byte[] attachment, string attachmentFileName)
        {
            try
            {
                _logger.LogInformation("=== EMAIL SERVICE CONFIGURATION ===");
                _logger.LogInformation($"SMTP Server: {_configuration.SmtpServer}:{_configuration.SmtpPort}");
                _logger.LogInformation($"Username: {_configuration.SmtpUsername}");
                _logger.LogInformation($"From Email: {_configuration.FromEmail}");
                _logger.LogInformation($"Enable SSL: {_configuration.EnableSsl}");
                _logger.LogInformation($"To: {toEmail}");
                _logger.LogInformation($"Subject: {subject}");
                _logger.LogInformation($"Has Attachment: {attachment != null}");
                _logger.LogInformation("=================================");

                using (var client = new SmtpClient(_configuration.SmtpServer, _configuration.SmtpPort))
                {
                    client.EnableSsl = _configuration.EnableSsl;
                    client.Credentials = new NetworkCredential(_configuration.SmtpUsername, _configuration.SmtpPassword);
                    client.Timeout = 30000;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_configuration.FromEmail, _configuration.FromName ?? "TicketHouse"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                        BodyEncoding = System.Text.Encoding.UTF8,
                        Priority = MailPriority.Normal
                    };

                    // Add Content-Type header explicitly
                    mailMessage.Headers.Add("Content-Type", "text/html; charset=utf-8");

                    // Add other headers
                    mailMessage.Headers.Add("List-Unsubscribe", $"<mailto:unsubscribe@tickethouse.in?subject=unsubscribe>");
                    mailMessage.Headers.Add("Precedence", "bulk");
                    mailMessage.Headers.Add("X-Auto-Response-Suppress", "OOF, AutoReply");
                    mailMessage.Headers.Add("X-Mailer", "TicketHouse Mail System");
                    mailMessage.Headers.Add("X-Priority", "3");
                    mailMessage.Headers.Add("Message-ID", $"<{Guid.NewGuid().ToString()}@tickethouse.in>");

                    mailMessage.To.Add(toEmail);
                    mailMessage.ReplyToList.Add(new MailAddress(_configuration.FromEmail, "TicketHouse Support"));

                    // Add attachment if provided - FIXED: Don't use 'using' for the stream
                    if (attachment != null && !string.IsNullOrEmpty(attachmentFileName))
                    {
                        // Create memory stream WITHOUT 'using' - it will be disposed with the mail message
                        MemoryStream stream = new MemoryStream(attachment);
                        var attachment_file = new Attachment(stream, attachmentFileName, "image/png");
                        mailMessage.Attachments.Add(attachment_file);
                        _logger.LogInformation($"Added attachment: {attachmentFileName}, Size: {attachment.Length} bytes");
                    }

                    // Send the email
                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Email sent successfully to {toEmail}");

                    // Clean up attachments (this will also dispose the streams)
                    if (mailMessage.Attachments.Count > 0)
                    {
                        foreach (var att in mailMessage.Attachments)
                        {
                            att.Dispose();
                        }
                    }

                    return true;
                }
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, $"✗ SMTP Error sending to {toEmail}");
                _logger.LogError($"Status Code: {smtpEx.StatusCode}");
                _logger.LogError($"Message: {smtpEx.Message}");

                if (smtpEx.InnerException != null)
                {
                    _logger.LogError($"Inner Exception: {smtpEx.InnerException.Message}");
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                return false;
            }
        }

        // Helper method to strip HTML for plain text version
        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Simple HTML stripping - in production use HtmlAgilityPack
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }

        //public async Task<bool> SendOTPEmailAsync(string toEmail, string otp, string userName = "")
        //{
        //    var subject = "Your OTP for TicketHouse";
        //    var body = $@"
        //        <!DOCTYPE html>
        //        <html>
        //        <head>
        //            <style>
        //                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        //                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
        //                .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        //                .otp-code {{ font-size: 32px; font-weight: bold; text-align: center; margin: 30px 0; color: #667eea; }}
        //                .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; text-align: center; color: #666; }}
        //            </style>
        //        </head>
        //        <body>
        //            <div class='container'>
        //                <div class='header'>
        //                    <h1>TicketHouse</h1>
        //                </div>
        //                <h2>Hello {(string.IsNullOrEmpty(userName) ? "User" : userName)}!</h2>
        //                <p>Your One-Time Password (OTP) for verification is:</p>
        //                <div class='otp-code'>{otp}</div>
        //                <p>This OTP is valid for 2 minutes. Please do not share this code with anyone.</p>
        //                <p>If you didn't request this OTP, please ignore this email.</p>
        //                <div class='footer'>
        //                    <p>Thank you for choosing TicketHouse!</p>
        //                    <p><strong>TicketHouse Team</strong></p>
        //                </div>
        //            </div>
        //        </body>
        //        </html>";

        //    return await SendEmailAsync(toEmail, subject, body);
        //}

        //public async Task<bool> SendOTPEmailAsync(string toEmail, string otp, string userName = "")
        //{
        //    var subject = "Your OTP for TicketHouse";
        //    var body = $@"
        //                <!DOCTYPE html>
        //                <html>
        //                <head>
        //                    <meta charset=""UTF-8"">
        //                    <title>OTP Verification - TicketHouse</title>
        //                    <style>
        //                        body {{
        //                            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
        //                            margin: 0;
        //                            padding: 20px;
        //                            background-color: #f5f5f5;
        //                            color: #1e293b;
        //                            line-height: 1.6;
        //                        }}

        //                        .container {{
        //                            max-width: 600px;
        //                            margin: 0 auto;
        //                            background-color: #ffffff;
        //                            border-radius: 16px;
        //                            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
        //                            border-collapse: collapse;
        //                            width: 100%;
        //                        }}

        //                        .header {{
        //                            padding: 40px 30px;
        //                            background: linear-gradient(135deg, rgb(146, 52, 234) 0%, rgb(226, 54, 163) 50%, rgb(239, 67, 67) 100%);
        //                            text-align: center;
        //                            border-radius: 16px 16px 0 0;
        //                        }}

        //                        .header h1 {{
        //                            margin: 0 0 10px 0;
        //                            font-size: 32px;
        //                            font-weight: 800;
        //                            color: #ffffff;
        //                        }}

        //                        .header p {{
        //                            margin: 0;
        //                            font-size: 16px;
        //                            color: rgba(255, 255, 255, 0.9);
        //                        }}

        //                        .content {{
        //                            padding: 30px;
        //                        }}

        //                        .otp-badge {{
        //                            background: linear-gradient(135deg, rgba(146, 52, 234, 0.1), rgba(226, 54, 163, 0.1), rgba(239, 67, 67, 0.1));
        //                            padding: 12px 30px;
        //                            border-radius: 40px;
        //                            font-size: 18px;
        //                            font-weight: 700;
        //                            display: inline-block;
        //                        }}

        //                        .section-title {{
        //                            margin: 0 0 15px 0;
        //                            font-size: 20px;
        //                            font-weight: 700;
        //                            background: linear-gradient(135deg, rgb(146, 52, 234) 0%, rgb(226, 54, 163) 50%, rgb(239, 67, 67) 100%);
        //                            -webkit-background-clip: text;
        //                            -webkit-text-fill-color: transparent;
        //                            background-clip: text;
        //                        }}

        //                        .otp-code {{
        //                            font-size: 48px;
        //                            font-weight: 800;
        //                            text-align: center;
        //                            margin: 30px 0;
        //                            letter-spacing: 10px;
        //                            background: linear-gradient(135deg, rgb(146, 52, 234) 0%, rgb(226, 54, 163) 50%, rgb(239, 67, 67) 100%);
        //                            -webkit-background-clip: text;
        //                            -webkit-text-fill-color: transparent;
        //                            background-clip: text;
        //                        }}

        //                        .otp-box {{
        //                            background: linear-gradient(135deg, rgba(146, 52, 234, 0.05), rgba(226, 54, 163, 0.05), rgba(239, 67, 67, 0.05));
        //                            padding: 30px;
        //                            border-radius: 12px;
        //                            text-align: center;
        //                            margin: 25px 0;
        //                        }}

        //                        .timer-box {{
        //                            background-color: #000000;
        //                            padding: 20px;
        //                            border-radius: 12px;
        //                            text-align: center;
        //                            margin: 25px 0;
        //                        }}

        //                        .timer-label {{
        //                            margin: 0 0 5px 0;
        //                            color: rgba(255, 255, 255, 0.9);
        //                            font-size: 14px;
        //                        }}

        //                        .timer-value {{
        //                            margin: 0;
        //                            font-size: 24px;
        //                            font-weight: 700;
        //                            color: #ffffff;
        //                        }}

        //                        .warning-box {{
        //                            background-color: #fff9e6;
        //                            padding: 20px;
        //                            border-radius: 12px;
        //                            border: 1px solid #ffd700;
        //                            margin: 25px 0;
        //                        }}

        //                        .greeting {{
        //                            font-size: 18px;
        //                            font-weight: 600;
        //                            margin-bottom: 20px;
        //                            color: #1e293b;
        //                        }}

        //                        .footer {{
        //                            background-color: #000000;
        //                            padding: 30px;
        //                            text-align: center;
        //                            border-radius: 0 0 16px 16px;
        //                        }}

        //                        .footer-logo {{
        //                            margin: 0 0 10px 0;
        //                            font-size: 24px;
        //                            font-weight: 800;
        //                            background: linear-gradient(135deg, rgb(146, 52, 234) 0%, rgb(226, 54, 163) 50%, rgb(239, 67, 67) 100%);
        //                            -webkit-background-clip: text;
        //                            -webkit-text-fill-color: transparent;
        //                            background-clip: text;
        //                        }}

        //                        .footer-text {{
        //                            margin: 5px 0;
        //                            color: rgba(255, 255, 255, 0.7);
        //                        }}

        //                        .footer-copyright {{
        //                            margin: 5px 0;
        //                            color: rgba(255, 255, 255, 0.5);
        //                            font-size: 12px;
        //                        }}
        //                    </style>
        //                </head>
        //                <body>
        //                    <div class=""container"">
        //                        <!-- Header -->
        //                        <div class=""header"">
        //                            <h1>🔐 VERIFICATION OTP</h1>
        //                            <p>Your security code for TicketHouse</p>
        //                        </div>

        //                        <!-- Content -->
        //                        <div class=""content"">
        //                            <!-- OTP Badge -->
        //                            <div style=""text-align: center; margin-bottom: 25px;"">
        //                                <span class=""otp-badge"">
        //                                    One-Time Password
        //                                </span>
        //                            </div>

        //                            <!-- Greeting -->
        //                            <div class=""greeting"">
        //                                Hello {(string.IsNullOrEmpty(userName) ? "User" : userName)}!
        //                            </div>

        //                            <!-- OTP Box -->
        //                            <div class=""otp-box"">
        //                                <p style=""margin: 0 0 10px 0; color: #64748b;"">Your One-Time Password (OTP) is:</p>
        //                                <div class=""otp-code"">{otp}</div>
        //                                <p style=""margin: 10px 0 0 0; color: #64748b;"">Please enter this code to complete your verification</p>
        //                            </div>

        //                            <!-- Timer Box -->
        //                            <div class=""timer-box"">
        //                                <p class=""timer-label"">⏰ OTP Validity</p>
        //                                <p class=""timer-value"">2 Minutes</p>
        //                                <p class=""timer-label"" style=""margin-top: 5px;"">Valid for next 2 minutes only</p>
        //                            </div>

        //                            <!-- Important Notes -->
        //                            <div class=""warning-box"">
        //                                <p style=""margin: 0 0 10px 0; font-weight: 700; color: #1e293b;"">⚠️ Security Alert:</p>
        //                                <ul style=""margin: 0; padding-left: 20px; color: #64748b;"">
        //                                    <li style=""margin-bottom: 8px;"">Never share this OTP with anyone</li>
        //                                    <li style=""margin-bottom: 8px;"">TicketHouse will never ask for your OTP</li>
        //                                    <li style=""margin-bottom: 8px;"">This OTP is valid for 2 minutes only</li>
        //                                    <li style=""margin-bottom: 8px;"">If you didn't request this, please ignore this email</li>
        //                                </ul>
        //                            </div>
        //                        </div>

        //                        <!-- Footer -->
        //                        <div class=""footer"">
        //                            <p class=""footer-logo"">TicketHouse</p>
        //                            <p class=""footer-text"">Your security is our priority</p>
        //                            <p class=""footer-copyright"">© {DateTime.Now.Year} TicketHouse. All rights reserved.</p>
        //                        </div>
        //                    </div>
        //                </body>
        //                </html>";

        //    return await SendEmailAsync(toEmail, subject, body);
        //}

        // Add to your EmailService.cs
        public async Task<bool> TestSmtpConnectionAsync()
        {
            try
            {
                using (var client = new SmtpClient(_configuration.SmtpServer, _configuration.SmtpPort))
                {
                    client.EnableSsl = _configuration.EnableSsl;
                    client.Credentials = new NetworkCredential(_configuration.SmtpUsername, _configuration.SmtpPassword);
                    client.Timeout = 15000; // 15 seconds for test

                    await client.SendMailAsync(
                        new MailMessage(_configuration.FromEmail, _configuration.FromEmail)
                        {
                            Subject = "SMTP Test",
                            Body = "Test connection",
                            IsBodyHtml = false
                        }
                    );

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP Connection Test Failed");
                return false;
            }
        }

        // Add to your EmailService class
        //public async Task<bool> SendForgotPasswordOTPEmailAsync(string email, string otp, string userName)
        //{
        //    try
        //    {
        //        var subject = "Password Reset OTP - TicketHouse";

        //        var body = $@"
        //        <html>
        //        <head>
        //            <style>
        //                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        //                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        //                .header {{ background: linear-gradient(135deg, #9234ea 0%, #e236a3 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        //                .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        //                .otp-code {{ font-size: 36px; font-weight: bold; color: #9234ea; text-align: center; padding: 20px; background: white; border-radius: 10px; margin: 20px 0; }}
        //                .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #999; }}
        //            </style>
        //        </head>
        //        <body>
        //            <div class='container'>
        //                <div class='header'>
        //                    <h2>Password Reset Request</h2>
        //                </div>
        //                <div class='content'>
        //                    <p>Hello {userName},</p>
        //                    <p>We received a request to reset your password for your TicketHouse account. Use the following OTP to proceed:</p>

        //                    <div class='otp-code'>{otp}</div>

        //                    <p>This OTP will expire in <strong>2 minutes</strong>.</p>

        //                    <p>If you didn't request a password reset, please ignore this email or contact support if you have concerns.</p>

        //                    <p>Best regards,<br>The TicketHouse Team</p>
        //                </div>
        //                <div class='footer'>
        //                    <p>This is an automated message, please do not reply to this email.</p>
        //                </div>
        //            </div>
        //        </body>
        //        </html>";

        //        return await SendEmailAsync(email, subject, body);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Failed to send forgot password OTP email: {ex.Message}");
        //        return false;
        //    }
        //}

        public async Task<bool> SendOTPEmailAsync(string toEmail, string otp, string userName = "")
        {
            var subject = "Verify Your Email - TicketHouse";
            var body = $@"
                        <!DOCTYPE html>
                        <html lang=""en"">
                        <head>
                            <meta charset=""UTF-8"" />
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
                            <title>Email Verification - TicketHouse</title>
                        </head>
                        <body style=""margin: 0; padding: 20px; background-color: #f4f4f6; font-family: 'Inter', 'Segoe UI', 'Helvetica Neue', Helvetica, Arial, sans-serif;"">

                            <!-- Main Container -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width: 520px; margin: 0 auto; background: #ffffff; border-radius: 18px; box-shadow: 0 10px 30px rgba(0,0,0,0.08); border-collapse: collapse;"">
                                <tr>
                                    <td style=""padding: 0;"">

                                        <!-- Header with Image -->
                                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background: linear-gradient(90deg, #000000, #111111); border-collapse: collapse;"">
                                            <tr>
                                                <td style=""padding: 20px;"">
                                                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                                                        <tr>
                                                            <td align=""left"" style=""padding: 0;"">
                                                                <img src=""https://tickethouse.in/assets/th_transparent_logo.png"" alt=""TicketHouse"" style=""display: block; border: 0; width: 60px; height: auto;"">
                                                            </td>
                                                            <td align=""right"" style=""padding: 0;"">
                                                                <div style=""font-size: 12px; text-align: right; color: #bbb; line-height: 1.4;"">
                                                                    VERIFICATION<br>
                                                                    <span style=""font-weight: 600; color: #fff;"">OTP</span>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- OTP Banner -->
                                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background: linear-gradient(90deg, #8e2de2, #ff416c); border-collapse: collapse;"">
                                            <tr>
                                                <td style=""padding: 20px; color: #ffffff;"">
                                                    <h3 style=""font-size: 18px; margin: 0 0 6px 0; font-weight: 600;"">Dear {(string.IsNullOrEmpty(userName) ? "there" : userName)}!</h3>
                                                    <p style=""font-size: 13px; margin: 0; color: #ffffff; opacity: 0.9;"">Kindly enter the OTP below to verify your email.</p>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- OTP Card -->
                                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""padding: 20px; border-collapse: collapse;"">
                                            <tr>
                                                <td style=""border-radius: 12px; padding: 20px;"">

                                                    <!-- OTP Badge -->
                                                    <div style=""text-align: center; margin-bottom: 20px;"">
                                                        <span style=""background: linear-gradient(135deg, rgba(146, 52, 234, 0.1), rgba(226, 54, 163, 0.1), rgba(239, 67, 67, 0.1)); padding: 8px 20px; border-radius: 40px; font-size: 14px; font-weight: 600; color: #8e2de2;"">
                                                            ONE-TIME PASSWORD
                                                        </span>
                                                    </div>

                                                    <h4 style=""font-size: 16px; margin: 0 0 20px 0; color: #1a1a1a; text-align: center;"">Email Verification Code</h4>

                                                    <!-- OTP Code Display -->
                                                    <div style=""background: linear-gradient(135deg, rgba(146, 52, 234, 0.05), rgba(226, 54, 163, 0.05), rgba(239, 67, 67, 0.05)); padding: 15px; border-radius: 12px; text-align: center; margin-top: 20px;"">
                                                        <div style=""font-size: 48px; font-weight: 800; letter-spacing: 10px; background: linear-gradient(135deg, #8e2de2, #ff416c); -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text; margin-bottom: 10px; line-height: 48px;"">{otp}</div>
                                                        <p style=""margin: 0; color: #666; font-size: 14px;"">Use this code to complete verification</p>
                                                        <p style=""margin: 0; color: #666; font-size: 16px;"">OTP will be valid for next 2 minutes only</p>
                                                    </div>
                                                </td>
                                            </tr>
                                             <tr>
                                                <td style=""padding: 20px;"">
                                                    <a href=""mailto:support@tickethouse.in"" target=""_blank"" style=""display: block; width: 100%; padding: 14px 20px; border-radius: 12px; border: none; font-weight: 600; font-size: 15px; background: linear-gradient(90deg, #8e2de2, #ff416c); color: #fff; text-decoration: none; text-align: center; box-sizing: border-box;"">
                                                        📧 Need Help? Contact Support
                                                    </a>
                                                </td>
                                            </tr>
                                            <tr style=""background:#000000;color:#fff;text-align:center;border-collapse:collapse;margin-top:10px"">
                                                <td style=""padding: 24px 20px;"">
                                                    <h4 style=""margin: 0 0 12px 0; font-size: 24px; font-weight: 800; background: linear-gradient(135deg, #8e2de2, #ff416c); -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text;"">TicketHouse</h4>
                                                    <p style=""margin: 0 0 6px 0; font-size: 13px; color: #ccc;"">Your security is our priority</p>
                                                    <p style=""margin: 0; font-size: 11px; color: #888;"">© {DateTime.Now.Year} Zentro Technologies LLP. All Rights Reserved.</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </body>
                        </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendForgotPasswordOTPEmailAsync(string email, string otp, string userName)
        {
            var subject = "Reset Your Password - TicketHouse";
            var body = $@"
                        <!DOCTYPE html>
                        <html lang=""en"">
                        <head>
                            <meta charset=""UTF-8"" />
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
                            <title>Password Reset - TicketHouse</title>
                        </head>
                        <body style=""margin: 0; padding: 20px; background-color: #f4f4f6; font-family: 'Inter', 'Segoe UI', 'Helvetica Neue', Helvetica, Arial, sans-serif;"">

                            <!-- Main Container -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width: 520px; margin: 0 auto; background: #ffffff; border-radius: 18px; box-shadow: 0 10px 30px rgba(0,0,0,0.08); border-collapse: collapse;"">
                                <tr>
                                    <td style=""padding: 0;"">

                                        <!-- Header with Image -->
                                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background: linear-gradient(90deg, #000000, #111111); border-collapse: collapse;"">
                                            <tr>
                                                <td style=""padding: 20px;"">
                                                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                                                        <tr>
                                                            <td align=""left"" style=""padding: 0;"">
                                                                <img src=""https://tickethouse.in/assets/th_transparent_logo.png"" alt=""TicketHouse"" style=""display: block; border: 0; width: 60px; height: auto;"">
                                                            </td>
                                                            <td align=""right"" style=""padding: 0;"">
                                                                <div style=""font-size: 12px; text-align: right; color: #bbb; line-height: 1.4;"">
                                                                    PASSWORD RESET<br>
                                                                    <span style=""font-weight: 600; color: #fff;"">OTP</span>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- OTP Banner -->
                                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background: linear-gradient(90deg, #8e2de2, #ff416c); border-collapse: collapse;"">
                                            <tr>
                                                <td style=""padding: 20px; color: #ffffff;"">
                                                    <h3 style=""font-size: 18px; margin: 0 0 6px 0; font-weight: 600;"">Dear {userName}!</h3>
                                                    <p style=""font-size: 13px; margin: 0; color: #ffffff; opacity: 0.9;"">We received a request to reset your password.</p>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- OTP Card -->
                                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""padding: 20px; border-collapse: collapse;"">
                                            <tr>
                                                <td style=""border-radius: 12px; padding: 20px;"">

                                                    <!-- OTP Badge -->
                                                    <div style=""text-align: center; margin-bottom: 20px;"">
                                                        <span style=""background: linear-gradient(135deg, rgba(146, 52, 234, 0.1), rgba(226, 54, 163, 0.1), rgba(239, 67, 67, 0.1)); padding: 8px 20px; border-radius: 40px; font-size: 14px; font-weight: 600; color: #8e2de2;"">
                                                            ONE-TIME PASSWORD
                                                        </span>
                                                    </div>

                                                    <h4 style=""font-size: 16px; margin: 0 0 20px 0; color: #1a1a1a; text-align: center;"">Email Verification Code</h4>

                                                    <!-- OTP Code Display -->
                                                    <div style=""background: linear-gradient(135deg, rgba(146, 52, 234, 0.05), rgba(226, 54, 163, 0.05), rgba(239, 67, 67, 0.05)); padding: 15px; border-radius: 12px; text-align: center; margin-top: 20px;"">
                                                        <div style=""font-size: 48px; font-weight: 800; letter-spacing: 10px; background: linear-gradient(135deg, #8e2de2, #ff416c); -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text; margin-bottom: 10px; line-height: 48px;"">{otp}</div>
                                                        <p style=""margin: 0; color: #666; font-size: 14px;"">Use this code to reset your password.</p>
                                                        <p style=""margin: 0; color: #666; font-size: 16px;"">OTP will be valid for next 2 minutes only</p>
                                                    </div>
                                                </td>
                                            </tr>
                                             <tr>
                                                <td style=""padding: 20px;"">
                                                    <a href=""mailto:support@tickethouse.in"" target=""_blank"" style=""display: block; width: 100%; padding: 14px 20px; border-radius: 12px; border: none; font-weight: 600; font-size: 15px; background: linear-gradient(90deg, #8e2de2, #ff416c); color: #fff; text-decoration: none; text-align: center; box-sizing: border-box;"">
                                                        📧 Need Help? Contact Support
                                                    </a>
                                                </td>
                                            </tr>
                                            <tr style=""background:#000000;color:#fff;text-align:center;border-collapse:collapse;margin-top:10px"">
                                                <td style=""padding: 24px 20px;"">
                                                    <h4 style=""margin: 0 0 12px 0; font-size: 24px; font-weight: 800; background: linear-gradient(135deg, #8e2de2, #ff416c); -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text;"">TicketHouse</h4>
                                                    <p style=""margin: 0 0 6px 0; font-size: 13px; color: #ccc;"">Your security is our priority</p>
                                                    <p style=""margin: 0; font-size: 11px; color: #888;"">© {DateTime.Now.Year} Zentro Technologies LLP. All Rights Reserved.</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </body>
                        </html>";

            return await SendEmailAsync(email, subject, body);
        }
    }
}
