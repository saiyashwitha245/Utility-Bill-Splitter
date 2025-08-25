using System.Net;
using System.Net.Mail;
using UtilityBillSplitterAPI.Interfaces;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var mail = new MailMessage
            {
                From = new MailAddress(_config["EmailSettings:Sender"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            using (var smtpClient = new SmtpClient(_config["SMTP:Host"], int.Parse(_config["SMTP:Port"])))
            {
                smtpClient.Credentials = new NetworkCredential(
                    _config["SMTP:Username"],
                    _config["SMTP:Password"]
                );
                smtpClient.EnableSsl = true;

                await smtpClient.SendMailAsync(mail);
            }

            return true;
        }
        catch (Exception ex)
        {
            //  Catch block with detailed error logging
            Console.WriteLine("📧 Email sending failed.");
            Console.WriteLine($"🔴 Error Message: {ex.Message}");
            Console.WriteLine($"🔍 Stack Trace: {ex.StackTrace}");

            // Optionally log to file or monitoring service here
            return false;
        }
    }
}


