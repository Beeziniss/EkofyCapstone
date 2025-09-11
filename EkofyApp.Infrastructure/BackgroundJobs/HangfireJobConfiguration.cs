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
            //RecurringJob.AddOrUpdate("test", () => app.Services.GetService<IBackgoundService>().DisplayLogTest(null), "* * * * *");

            //RecurringJob.AddOrUpdate("email_sending_once",() => app.Services.GetService<IBackgoundService>().SendEmail("satori562003@gmail.com"), "33 * * * *");

            BackgroundJob.Enqueue(() => app.Services.GetService<IBackgoundService>()!.SendEmail("satori562003@gmail.com"));

            
        }
    }
}
