using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Works;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Jobs;
using EkofyApp.Application.ServiceInterfaces.MonthlyStreamCounts;
using EkofyApp.Application.ServiceInterfaces.Reports;
using EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Utils;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Serilog;
using StackExchange.Redis;
using System.Net;
using System.Net.Mail;

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
    public void SendEmailJob(EmailTemplateType templateType, string toEmail, params string[] parameters)
    {
        try
        {
            SmtpClient smtpClient = new(Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST"))
            {
                Port = Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT") != null ? int.Parse(Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT")!) : 587,

                Credentials = new NetworkCredential(Environment.GetEnvironmentVariable("EMAIL_SMTP_USERNAME"), Environment.GetEnvironmentVariable("EMAIL_SMTP_PASSWORD")),

                EnableSsl = true,
            };

            // Lấy template và subject dựa vào enum
            Func<string[], string> emailTemplate = EmailTemplateFactory.GetTemplate(templateType);
            string subject = EmailTemplateFactory.GetSubject(templateType);

            MailMessage mailMessage = new()
            {
                From = new MailAddress(Environment.GetEnvironmentVariable("EMAIL_SMTP_USERNAME")!),
                Subject = subject,
                Body = emailTemplate.Invoke(parameters),
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


    [Queue("track_upload")]
    [JobDisplayName("Release Scheduled Track")]
    public async Task ReleaseScheduledTrackJob(string trackId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var trackService = scope.ServiceProvider.GetRequiredService<ITrackService>();
        await trackService.ReleaseScheduledTrackAsync(trackId);
    }

    [Queue("scheduled")]
    [JobDisplayName("Scheduled Job Example")]
    public async Task MonthlyRoyaltyReportJob()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var royaltyReportService = scope.ServiceProvider.GetRequiredService<IRoyaltyReportService>();
        await royaltyReportService.GenerateMonthlyRoyaltyReportsAsync(HelperMethod.GetUtcPlus7TimeOffset().Month, HelperMethod.GetUtcPlus7TimeOffset().Year);
    }

    #region Stream Count Job
    [Queue("track_count")]
    [JobDisplayName("Update Stream Count")]
    public async Task UpdateStreamCountJob()
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            //tạo 
            var redis = scope.ServiceProvider.GetRequiredService<IRedisCacheService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var monthlyStreamCountService = scope.ServiceProvider.GetRequiredService<IMonthlyStreamCountService>();

            string pattern = "stream_count:*";
            string[] keyList = redis.GetAllKeysByPattern(pattern);

            //check xem mảng có empty hay không
            if (keyList.Length == 0)
            {
                return;
            }
            var tasks = keyList.Select(async key =>
            {
                await UpdateIntoMongoDB(key, redis, unitOfWork, monthlyStreamCountService);
            });
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.Error("Error in UpdateStreamCountJob: {ErrorMessage}", ex.Message);
        }

    }

    private async Task UpdateIntoMongoDB(string key, IRedisCacheService redis, IUnitOfWork unitOfWork, IMonthlyStreamCountService monthlyStreamCountService)
    {

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

                await monthlyStreamCountService.UpsertMonthlyStreamCountAsync(trackId, playedCount, HelperMethod.GetUtcPlus7TimeOffset().Month, HelperMethod.GetUtcPlus7TimeOffset().Year);
            }
        }
    }
    #endregion

    [Queue("expired_restriction")]
    [JobDisplayName("Update Restriction User's Status")]
    public async Task RemoveExpiredRestrictionAsync(string userId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        await reportService.RemoveExpiredRestrictionAsync(userId);
    }

    [Queue("progressing_upload")]
    [JobDisplayName("Check Progressing Uploads")]
    public async Task CheckProgressingUploadsJob(Stream stream, CreateTrackRequest createTrackRequest, CreateWorkRequest createWorkRequest, CreateRecordingRequest createRecordingRequest)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var trackService = scope.ServiceProvider.GetRequiredService<ITrackService>();
        await trackService.ApproveAutomaticallyAsync(stream, createTrackRequest, createWorkRequest, createRecordingRequest);
    }
}
