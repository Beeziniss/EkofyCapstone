using EkofyApp.Application.ServiceInterfaces.Jobs;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using System.Net;
using System.Net.Mail;

namespace EkofyApp.Infrastructure.Services.Jobs
{
    public class BackgoundService : IBackgoundService
    {
        //[Queue("default")]
        [JobDisplayName("Test display Background Job")]
        public void DisplayLogTest(PerformContext context)
        {
            Console.WriteLine("Test Background Job is running...");
            Console.WriteLine($"Test Background Job has completed: {DateTime.Now}");
        }


        [Queue("default")]
        [JobDisplayName("Send Email")]
        public void SendEmail(string toEmail)
        {
            try
            {
                SmtpClient smtpClient = new SmtpClient(Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST"))
                {
                    Port = Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT") != null ? int.Parse(Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT")!) : 587,

                    Credentials = new NetworkCredential(Environment.GetEnvironmentVariable("EMAIL_SMTP_USERNAME"), Environment.GetEnvironmentVariable("EMAIL_SMTP_PASSWORD")),

                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(Environment.GetEnvironmentVariable("EMAIL_SMTP_USERNAME")!),
                    Subject = "Test gửi email từ EkofyApp",
                    Body = "<h1>Chào bạn, đây là email được gửi từ EkofyApp</h1><p>Đây là nội dung của email.</p>",
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);

                mailMessage.Headers.Add("X-Priority", "1");
                mailMessage.Headers.Add("X-MSMail-Priority", "High");
                mailMessage.Headers.Add("Importance", "High");

                smtpClient.Send(mailMessage);
                Console.WriteLine($"Email sent to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email to {toEmail}. Error: {ex.Message}");
            }
        }
    }
}
