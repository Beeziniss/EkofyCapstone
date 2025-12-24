using EkofyApp.Application.ServiceInterfaces.Jobs;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EkofyApp.Infrastructure.BackgroundJobs;

public static class HangfireJobConfiguration
{
    public static void ConfigureJobs(this WebApplication app)
    {
        string timeZoneId = OperatingSystem.IsWindows()
            ? "SE Asia Standard Time"
            : "Asia/Ho_Chi_Minh"; // Linux / macOS dùng IANA

        TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        RecurringJob.AddOrUpdate("add-stream-count", () => app.Services.GetService<IBackgoundService>()!.UpdateStreamCountJob(), "*/3 * * * *", new RecurringJobOptions { TimeZone = vietnamTimeZone });

        RecurringJob.AddOrUpdate("monthly-royalty-report", () => app.Services.GetService<IBackgoundService>()!.MonthlyRoyaltyReportJob(), "59 23 L * ?", new RecurringJobOptions { TimeZone = vietnamTimeZone });

        // Tạo daily playlist vào lúc 7 giờ sáng (GMT +7) mỗi ngày
        RecurringJob.AddOrUpdate("daily-playlist-generation", () => app.Services.GetService<IBackgoundService>()!.DailyPlaylistGenerationJob(), Cron.Daily(7, 0), new RecurringJobOptions { TimeZone = vietnamTimeZone });

        // Kiểm tra và xử lý các yêu cầu upload lâu ngày chưa được duyệt
        RecurringJob.AddOrUpdate("escalate-old-upload-requests", () => app.Services.GetService<IBackgoundService>()!.EscalateOldUploadRequestsJob(), Cron.Daily(0, 0), new RecurringJobOptions { TimeZone = vietnamTimeZone });

        //Kiểm tra và gửi thông báo sắp tới hạn order trong mỗi 1 tiếng
        RecurringJob.AddOrUpdate("notify-order-in-24-hours", () => app.Services.GetService<IBackgoundService>()!.NotifyOrderBeforeDeadlineJob(), "* */1 * * *", new RecurringJobOptions { TimeZone = vietnamTimeZone });
    }
}
