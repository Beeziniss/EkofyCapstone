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
            app.Services.CreateScope();

            //BackgroundJob.Enqueue(() => app.Services.GetService<IBackgoundService>()!.SendEmail("satori562003@gmail.com"));

            //RecurringJob.AddOrUpdate("add-count-stream", () => app.Services.GetService<IBackgoundService>()!.UpdateStreamCountJob(), "*/5 * * * *");
        }
    }
}
