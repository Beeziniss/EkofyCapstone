using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Jobs;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using Hangfire;
using Hangfire.Console;
using Hangfire.Logging;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using StackExchange.Redis;
using Stripe.Forwarding;
using System.Net;
using System.Net.Mail;
using static System.Formats.Asn1.AsnWriter;

namespace EkofyApp.Infrastructure.Services.Jobs;
public class BackgoundService : IBackgoundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger _logger;

    public BackgoundService(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _serviceScopeFactory = scopeFactory;
        _logger = logger;
    }

    //[Queue("default")]
    [JobDisplayName("Test display Background Job")]
    public void DisplayLogTest(PerformContext context)
    {
        Console.WriteLine("Test Background Job is running...");
        Console.WriteLine($"Test Background Job has completed: {DateTime.Now}");
    }


    [Queue("default")]
    [JobDisplayName("Send Email")]
    public void SendEmailJob(string toEmail)
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


    #region Stream Count Job
    [Queue("default")]
    [JobDisplayName("Update Stream Count")]
    public async Task UpdateStreamCountJob()
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var redis = scope.ServiceProvider.GetRequiredService<IRedisCacheService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            string pattern = "stream_count:*";
            string[] keyList = redis.GetAllKeysByPattern(pattern);

            //check xem mảng có empty hay không
            if (keyList.Length == 0)
            {
                return;
            }
            var tasks = keyList.Select(key => UpdateIntoMongoDB(key, redis, unitOfWork));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.Error("Error in UpdateStreamCountJob: {ErrorMessage}", ex.Message);
        }

    }

    private async Task UpdateIntoMongoDB(string key, IRedisCacheService redis, IUnitOfWork unitOfWork)
    {
        //string[] keyParts = key.Split(':');
        //string userId = keyParts[1];


        HashEntry[]? hashEntry = await redis.HashGetAllAsync(key);

        //check nếu hash từ key bị null thì return
        if (hashEntry is null || hashEntry.Length == 0)
        {
            return;
        }

        //update count trong từng track
        foreach (var entry in hashEntry)
        {
            string trackId = entry.Name!.ToString();

            int playedCount = (int)entry.Value;

            if (playedCount > 0)
            {
                UpdateDefinition<Track> updateDefinition = Builders<Track>.Update.Inc(track => track.StreamCount, playedCount);
                //update rồi giảm count trong redis để tránh update lại cái cũ cho lần sau
                await unitOfWork.GetCollection<Track>().UpdateOneAsync(rh => rh.Id == trackId, updateDefinition);
                await redis.HashDecrementAsync(key, trackId, playedCount);
            }
            //else
            //{
            //    continue;
            //}

        }
    }
    #endregion
}
