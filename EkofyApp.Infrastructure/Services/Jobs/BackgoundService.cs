using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Jobs;
using EkofyApp.Application.ServiceInterfaces.MonthlyStreamCounts;
using EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
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
    public void SendOtpEmailJob(string fullName, string toEmail, string otp)
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
                Subject = "Verify OTP",
                Body = @$"<!doctype html>
<html lang=""en"">
	<head>
		<meta charset=""UTF-8"" />
		<title></title>
	</head>

	<body
		style=""
			margin: 0;
			padding: 0;
			font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif;
			color: #333;
			background-color: #fff;
		""
	>
		<div
			class=""background-ekofy""
			style=""background: linear-gradient(45deg, #3b54ea 0%, #ab4ee5 100%); padding: 40px 0""
		>
			<div
				class=""container""
				style=""
					margin: 0 auto;
					padding: 64px 56px;
					width: 100%;
					max-width: 600px;
					background-color: #ffffff;
					border-radius: 32px;
					line-height: 1.8;
				""
			>
				<div class=""header"" style=""text-align: center"">
					<img src=""https://res.cloudinary.com/dofnn7sbx/image/upload/v1759760383/logo_yqjeui.png"" alt=""Ekofy Logo"" />
				</div>

				<p
					class=""separator""
					style=""height: 1px; width: 100%; background-color: #d9d9d9; margin: 32px 0""
				></p>

				<strong>Dear {fullName},</strong>
				<p>
					We have received a login request for your Ekofy account. For security purposes,
					please verify your identity by providing the following One-Time Password (OTP).
					<br />
					<b>Your One-Time Password (OTP) verification code is:</b>
				</p>
				<h2
					class=""otp""
					style=""
						background: linear-gradient(to right, #3b54ea 0%, #ab4ee5 100%);
						margin: 0 auto;
						width: max-content;
						padding: 0 10px;
						color: #fff;
						border-radius: 4px;
					""
				>
					{otp}
				</h2>
				<p style=""font-size: 0.9em"">
					<strong>One-Time Password (OTP) is valid for 3 minutes.</strong>
					<br />
					<br />
					If you did not initiate this login request, please disregard this message. Please ensure
					the confidentiality of your OTP and do not share it with anyone.<br />
					<strong>Do not forward or give this code to anyone.</strong>
					<br />
					<br />
					<strong>Thank you for using Ekofy.</strong>
					<br />
					<br />
					Best regards,
					<br />
					<strong>Beeziniss</strong>
				</p>
			</div>
		</div>
	</body>
</html>",
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
}
