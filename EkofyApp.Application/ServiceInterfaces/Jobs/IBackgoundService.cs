using Hangfire.Server;

namespace EkofyApp.Application.ServiceInterfaces.Jobs
{
    public interface IBackgoundService
    {
        void DisplayLogTest(PerformContext context);
        Task MonthlyRoyaltyReportJob();
        void SendOtpEmailJob(string fullName, string toEmail, string otp);
        Task UpdateStreamCountJob();
    }
}
