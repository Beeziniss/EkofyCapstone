using Hangfire.Server;

namespace EkofyApp.Application.ServiceInterfaces.Jobs
{
    public interface IBackgoundService
    {
        void DisplayLogTest(PerformContext context);
        void SendEmailJob(string toEmail);
        Task UpdateStreamCountJob();
    }
}
