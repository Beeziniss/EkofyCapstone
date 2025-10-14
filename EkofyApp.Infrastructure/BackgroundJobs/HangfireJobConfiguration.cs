using EkofyApp.Application.ServiceInterfaces.Jobs;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EkofyApp.Infrastructure.BackgroundJobs
{
    public static class HangfireJobConfiguration
    {
        public static void ConfigureJobs(this WebApplication app)
        {
            //app.Services.CreateScope();

            //BackgroundJob.Enqueue(() => app.Services.GetService<IBackgoundService>()!.SendEmailJob("satori562003@gmail.com"));

            //RecurringJob.AddOrUpdate("add-stream-count", () => app.Services.GetService<IBackgoundService>()!.UpdateStreamCountJob(), "*/3 * * * *");

            // ở đây hangfire lấy theo giờ của mongo nên để chạy vào 23:59 ngày cuối tháng thì phải trừ 7 tiếng (giờ VN là GMT+7)
            RecurringJob.AddOrUpdate("monthly-royalty-report", () => app.Services.GetService<IBackgoundService>()!.MonthlyRoyaltyReportJob(), "59 16 L * ?");
        }
    }
}
