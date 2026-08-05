using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;

namespace WebApplicationSampleTest2.Repository
{
  

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public void SendOTP(string toEmail, string otp, string purpose)
        {
            try
            {
                string subject = purpose switch
                {
                    "REGISTER" => "Email Verification OTP — Narwade Hospital",
                    "FORGOT" => "Password Reset OTP — Narwade Hospital",
                    _ => "OTP — Narwade Hospital"
                };

                string body = purpose switch
                {
                    "REGISTER" => $@"
                        <h3>Welcome to Narwade Hospital Patient Portal</h3>
                        <p>Your email verification OTP is:</p>
                        <h2 style='color:#1a5276;
                                   letter-spacing:8px'>
                            {otp}
                        </h2>
                        <p>Valid for <strong>10 minutes</strong>.</p>
                        <p>If you did not request this, ignore.</p>",

                    "FORGOT" => $@"
                        <h3>Password Reset Request</h3>
                        <p>Your password reset OTP is:</p>
                        <h2 style='color:#1a5276;
                                   letter-spacing:8px'>
                            {otp}
                        </h2>
                        <p>Valid for <strong>10 minutes</strong>.</p>
                        <p>If you did not request this, ignore.</p>",

                    _ => $"Your OTP is: {otp}"
                };

                var smtp = new SmtpClient
                {
                    Host = _config["EmailSettings:Host"],
                    Port = int.Parse(
                                    _config["EmailSettings:Port"]),
                    EnableSsl = true,
                    Credentials = new NetworkCredential(
                        _config["EmailSettings:Username"],
                        _config["EmailSettings:Password"])
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(
                        _config["EmailSettings:Username"],
                        "Narwade Hospital"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mail.To.Add(toEmail);
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                throw new Exception("Error sending email", ex);
            }
        }
    }
}