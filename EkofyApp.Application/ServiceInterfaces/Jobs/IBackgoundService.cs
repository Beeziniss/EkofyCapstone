using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Works;
using EkofyApp.Domain.Enums;
using Hangfire.Server;

namespace EkofyApp.Application.ServiceInterfaces.Jobs
{
    public interface IBackgoundService
    {
        void DisplayLogTest(PerformContext context);
        Task ReleaseScheduledTrackJob(string trackId);
        void SendEmailJob(EmailTemplateType templateType, string toEmail, params string[] parameters);
        Task MonthlyRoyaltyReportJob();
        Task UpdateStreamCountJob();
        Task RemoveExpiredRestrictionAsync(string userId);
        Task CheckProgressingUploadsJob(string userId, byte[] bytes, CreateTrackRequest createTrackRequest, CreateWorkRequest createWorkRequest, CreateRecordingRequest createRecordingRequest);
        Task ProcessTrackStreamingMetricJobAsync(string trackId, PopularityActionType actionType);
        Task ProcessTrackEngagementMetricJobAsync(string trackId, PopularityActionType actionType);
        Task ProcessTrackDiscoveryMetricJobAsync(string trackId, PopularityActionType actionType);
        Task ProcessArtistEngagementMetricJobAsync(string artistId, PopularityActionType actionType);
        Task ProcessArtistDiscoveryMetricJobAsync(string artistId, PopularityActionType actionType);
        Task<bool> CheckProgressingUploadsManuallyJob(string actionByUserId, string uploadId);
    }
}
