using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.Utils;
public sealed class EmailTemplateFactory
{
    public static Func<string[], string> GetTemplate(EmailTemplateType type)
    {
        return type switch
        {
            EmailTemplateType.VerifyOtp => HelperEmailTemplate.VerifyOtp,
            //EmailTemplateType.Welcome => HelperEmailTemplate.WelcomeTemplate,
            //EmailTemplateType.ResetPassword => HelperEmailTemplate.ResetPasswordTemplate,
            EmailTemplateType.RegisterNotification => HelperEmailTemplate.RegisterNotification,
            EmailTemplateType.RegisterApprove => HelperEmailTemplate.RegisterApprove,
            EmailTemplateType.RegisterReject => HelperEmailTemplate.RegisterReject,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static string GetSubject(EmailTemplateType type)
    {
        return type switch
        {
            EmailTemplateType.VerifyOtp => HelperEmailTemplate.SubjectVerifyOtp(),
            //EmailTemplateType.Welcome => "Welcome to Ekofy!",
            //EmailTemplateType.ResetPassword => "Reset your password",
            EmailTemplateType.RegisterNotification => HelperEmailTemplate.SubjectRegisterNotification(),
            EmailTemplateType.RegisterApprove => HelperEmailTemplate.SubjectRegisterReject(),
            EmailTemplateType.RegisterReject => HelperEmailTemplate.SubjectRegisterReject(),
            EmailTemplateType.ResetPasswordOtp => HelperEmailTemplate.SubjectResetPassword(),
            _ => "Ekofy Notification"
        };
    }
}
