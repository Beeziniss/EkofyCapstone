using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.Utils;
public sealed class EmailTemplateFactory
{
    public static Func<string[], string> GetTemplate(EmailTemplateType type)
    {
        return type switch
        {
            EmailTemplateType.VerifyOtp => HelperEmailTemplate.VerifyOtp,
            EmailTemplateType.RegisterNotification => HelperEmailTemplate.RegisterNotification,
            EmailTemplateType.RegisterApprove => HelperEmailTemplate.RegisterApprove,
            EmailTemplateType.RegisterReject => HelperEmailTemplate.RegisterReject,
            EmailTemplateType.ResetPasswordOtp => HelperEmailTemplate.ResetPasswordOtp,
            EmailTemplateType.PasswordChanged => HelperEmailTemplate.PasswordChanged,
            EmailTemplateType.WarningReport => HelperEmailTemplate.ReportWarning,
            EmailTemplateType.TemporarySuspension => HelperEmailTemplate.TemporarySuspension,
            EmailTemplateType.EntilementRestriction => HelperEmailTemplate.EntitlementRestriction,
            EmailTemplateType.TrackRemoval => HelperEmailTemplate.TrackRemoval,
            EmailTemplateType.RequestRemoval => HelperEmailTemplate.RequestRemoval,
            EmailTemplateType.CommentRemoval => HelperEmailTemplate.CommentRemoval,
            EmailTemplateType.PermanentBan => HelperEmailTemplate.PermanentBan,
            EmailTemplateType.Unban => HelperEmailTemplate.Unban,
            EmailTemplateType.SubscriptionCancelled => HelperEmailTemplate.SubscriptionCancelled,
            EmailTemplateType.SubscriptionResumed => HelperEmailTemplate.SubscriptionResumed,
            EmailTemplateType.SubscriptionExpired => HelperEmailTemplate.SubscriptionExpired,
            EmailTemplateType.UpdatePolicy => HelperEmailTemplate.UpdatePolicy,
            EmailTemplateType.OrderDeadlineReminder => HelperEmailTemplate.OrderDeadlineReminder,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static string GetSubject(EmailTemplateType type)
    {
        return type switch
        {
            EmailTemplateType.VerifyOtp => HelperEmailTemplate.SubjectVerifyOtp(),
            EmailTemplateType.RegisterNotification => HelperEmailTemplate.SubjectRegisterNotification(),
            EmailTemplateType.RegisterApprove => HelperEmailTemplate.SubjectRegisterApprove(),
            EmailTemplateType.RegisterReject => HelperEmailTemplate.SubjectRegisterReject(),
            EmailTemplateType.ResetPasswordOtp => HelperEmailTemplate.SubjectResetPassword(),
            EmailTemplateType.PasswordChanged => HelperEmailTemplate.SubjectPasswordChanged(),
            EmailTemplateType.WarningReport => HelperEmailTemplate.SubjectReportWarning(),
            EmailTemplateType.TemporarySuspension => HelperEmailTemplate.SubjectTemporarySuspension(),
            EmailTemplateType.EntilementRestriction => HelperEmailTemplate.SubjectEntitlementRestriction(),
            EmailTemplateType.TrackRemoval => HelperEmailTemplate.SubjectTrackRemoval(),
            EmailTemplateType.RequestRemoval => HelperEmailTemplate.SubjectRequestRemoval(),
            EmailTemplateType.CommentRemoval => HelperEmailTemplate.SubjectCommentRemoval(),
            EmailTemplateType.PermanentBan => HelperEmailTemplate.SubjectPermanentBan(),
            EmailTemplateType.Unban => HelperEmailTemplate.SubjectUnban(),
            EmailTemplateType.SubscriptionCancelled => HelperEmailTemplate.SubjectSubscriptionCancelled(),
            EmailTemplateType.SubscriptionResumed => HelperEmailTemplate.SubjectSubscriptionResumed(),
            EmailTemplateType.SubscriptionExpired => HelperEmailTemplate.SubjectSubscriptionExpired(),
            EmailTemplateType.UpdatePolicy => HelperEmailTemplate.SubjectUpdatePolicy(),
            EmailTemplateType.OrderDeadlineReminder => HelperEmailTemplate.OrderDeadlineReminder(),
            _ => "Ekofy Notification"
        };
    }
}
