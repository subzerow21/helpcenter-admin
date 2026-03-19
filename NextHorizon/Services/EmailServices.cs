using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NextHorizon.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpServer = _configuration["EmailSettings:SmtpServer"];
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            _smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            _smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            _fromEmail = _configuration["EmailSettings:FromEmail"];
            _fromName = _configuration["EmailSettings:FromName"];
        }

        public async Task<bool> SendOTPEmailAsync(string email, string name, string otpCode)
        {
            try
            {
                var subject = "Your Password Reset OTP Code - Next Horizon";
                var body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: 'Poppins', Arial, sans-serif; background-color: #f4f7f6; margin: 0; padding: 20px; }}
                            .container {{ max-width: 500px; margin: 0 auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.08); }}
                            .header {{ background: #000; color: white; padding: 30px; text-align: center; }}
                            .header h1 {{ font-size: 2.5rem; margin: 0; }}
                            .header p {{ font-size: 0.75rem; letter-spacing: 4px; text-transform: uppercase; opacity: 0.7; margin: 0; }}
                            .content {{ padding: 40px; text-align: center; }}
                            .otp-code {{ font-size: 36px; font-weight: 700; letter-spacing: 8px; color: #000; margin: 30px 0; padding: 20px; background: #f8f9fa; border-radius: 10px; }}
                            .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
                            .btn {{ background: #000; color: white; padding: 12px 30px; text-decoration: none; border-radius: 10px; display: inline-block; font-weight: 600; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>NH</h1>
                                <p>Next Horizon</p>
                            </div>
                            <div class='content'>
                                <h2>Hello {name},</h2>
                                <p>We received a request to reset your password. Use the OTP code below to proceed:</p>
                                <div class='otp-code'>{otpCode}</div>
                                <p>This code will expire in <strong>15 minutes</strong>.</p>
                                <p>If you didn't request this, please ignore this email or contact support.</p>
                            </div>
                            <div class='footer'>
                                <p>&copy; 2026 Next Horizon. All rights reserved.</p>
                                <p>This is an automated message, please do not reply.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendPasswordResetConfirmationAsync(string email, string name)
        {
            try
            {
                var subject = "Your Password Has Been Reset - Next Horizon";
                var body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: 'Poppins', Arial, sans-serif; background-color: #f4f7f6; margin: 0; padding: 20px; }}
                            .container {{ max-width: 500px; margin: 0 auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.08); }}
                            .header {{ background: #000; color: white; padding: 30px; text-align: center; }}
                            .header h1 {{ font-size: 2.5rem; margin: 0; }}
                            .header p {{ font-size: 0.75rem; letter-spacing: 4px; text-transform: uppercase; opacity: 0.7; margin: 0; }}
                            .content {{ padding: 40px; text-align: center; }}
                            .success-icon {{ font-size: 60px; color: #28a745; margin: 20px 0; }}
                            .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>NH</h1>
                                <p>Next Horizon</p>
                            </div>
                            <div class='content'>
                                <div class='success-icon'>✓</div>
                                <h2>Hello {name},</h2>
                                <p>Your password has been successfully reset.</p>
                                <p>You can now log in with your new password.</p>
                                <p style='margin-top: 30px;'>
                                    <a href='https://yourdomain.com/Login/AdminLogin' class='btn'>Go to Login</a>
                                </p>
                                <p style='margin-top: 30px; font-size: 14px; color: #6c757d;'>
                                    If you didn't make this change, please contact support immediately.
                                </p>
                            </div>
                            <div class='footer'>
                                <p>&copy; 2026 Next Horizon. All rights reserved.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_fromEmail, _fromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                        Priority = MailPriority.High
                    };

                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMTP Error: {ex.Message}");
                return false;
            }
        }
    }
}