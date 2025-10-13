namespace EkofyApp.Domain.Utils;
public sealed class HelperEmailTemplate
{
    public static string SubjectVerifyOtp() => "Ekofy - Verify OTP Code";
	public static string SubjectRegisterNotification() => "Ekofy - Registration Notification";
    public static string SubjectRegisterApprove() => "Ekofy - Registration Approved";
    public static string SubjectRegisterReject() => "Ekofy - Registration Rejected";
    public static string SubjectResetPassword() => "Ekofy - Reset Password OTP";
    public static string SubjectPasswordChanged() => "Ekofy - Password Changed";

	public static string RegisterNotification(string[] paramaters)
	{
		// Định nghĩa parameters
		string fullName = paramaters[0];
		string email = paramaters[1];

		return @$"<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <title>Artist Email Templates</title>
</head>
<body style=""font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; background-color: #f5f5f5; padding: 40px;"">

  <!-- APPROVE ARTIST -->
  <div style=""background: linear-gradient(45deg, #3b54ea 0%, #ab4ee5 100%); padding: 40px 0; margin-bottom: 40px"">
    <div style=""margin: 0 auto; padding: 64px 56px; width: 100%; max-width: 600px; background-color: #ffffff; border-radius: 32px; line-height: 1.8;"">
      <div style=""text-align: center"">
        <img src=""https://res.cloudinary.com/dofnn7sbx/image/upload/v1759760383/logo_yqjeui.png"" alt=""Ekofy Logo"" />
      </div>
      <div style=""height: 1px; width: 100%; background-color: #d9d9d9; margin: 32px 0""></div>
      <strong>Dear {fullName},</strong>
      <p>
        Thank you for registering as an artist on Ekofy.
      </p>
      <p>
        We have received your application associated with the email <strong>{email}</strong>. Our team is currently reviewing your details to ensure everything meets the requirements of our platform.
      </p>
      <p>
        This process usually takes a short time, and we appreciate your patience. You will receive a follow-up email once the review is complete.
      </p>
      <p>
        If you have any questions in the meantime, feel free to reach out to our support team.
      </p>
      <p style=""font-size: 0.9em"">
        Thank you for choosing <strong>Ekofy</strong>.<br><br>
        Best regards,<br>
        <strong>The Ekofy Team</strong>
      </p>
    </div>
  </div>

  </body>
</html>
";
	}

    public static string RegisterApprove(string[] paramaters)
    {
        // Định nghĩa parameters
        string fullName = paramaters[0];
        string email = paramaters[1];

        return @$"<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <title>Artist Email Templates</title>
</head>
<body style=""font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; background-color: #f5f5f5; padding: 40px;"">

  <!-- APPROVE ARTIST -->
  <div style=""background: linear-gradient(45deg, #3b54ea 0%, #ab4ee5 100%); padding: 40px 0; margin-bottom: 40px"">
    <div style=""margin: 0 auto; padding: 64px 56px; width: 100%; max-width: 600px; background-color: #ffffff; border-radius: 32px; line-height: 1.8;"">
      <div style=""text-align: center"">
        <img src=""https://res.cloudinary.com/dofnn7sbx/image/upload/v1759760383/logo_yqjeui.png"" alt=""Ekofy Logo"" />
      </div>
      <div style=""height: 1px; width: 100%; background-color: #d9d9d9; margin: 32px 0""></div>
      <strong>Dear {fullName},</strong>
      <p>
        We are excited to inform you that your artist application associated with the email <strong>{email}</strong> has been <strong>approved</strong>!
      </p>
      <p>
        You now have full access to the Ekofy platform as an artist. Start uploading your music, managing your profile, and connecting with your fans.
      </p>
      <p>
        If you have any questions or need assistance, feel free to reach out to our support team at any time.
      </p>
      <p style=""font-size: 0.9em"">
        Thank you for choosing <strong>Ekofy</strong>.<br><br>
        Best regards,<br>
        <strong>The Ekofy Team</strong>
      </p>
    </div>
  </div>
  
  </body>
</html>";
    }

    public static string RegisterReject(string[] paramaters)
    {
        // Định nghĩa parameters
        string fullName = paramaters[0];
        string email = paramaters[1];
        string reason = paramaters[2];

        return @$"<!doctype html>
<html lang=""en"">
<head>
  <!-- REJECT ARTIST -->
  <div style=""background: linear-gradient(45deg, #3b54ea 0%, #ab4ee5 100%); padding: 40px 0"">
    <div style=""margin: 0 auto; padding: 64px 56px; width: 100%; max-width: 600px; background-color: #ffffff; border-radius: 32px; line-height: 1.8;"">
      <div style=""text-align: center"">
        <img src=""https://res.cloudinary.com/dofnn7sbx/image/upload/v1759760383/logo_yqjeui.png"" alt=""Ekofy Logo"" />
      </div>
      <div style=""height: 1px; width: 100%; background-color: #d9d9d9; margin: 32px 0""></div>
      <strong>Dear {fullName},</strong>
      <p>
        We regret to inform you that your artist application associated with the email <strong>{email}</strong> has been <strong>rejected</strong>.
      </p>
      <p>
        <strong>Reason:</strong><br>
        {reason}
      </p>
      <p>
        If you believe this decision was made in error or if you would like to appeal, please contact our support team for further clarification.
      </p>
      <p style=""font-size: 0.9em"">
        Thank you for your interest in <strong>Ekofy</strong>.<br><br>
        Best regards,<br>
        <strong>The Ekofy Team</strong>
      </p>
    </div>
  </div>

</body>
</html>";
    }

    /// <summary>
    /// Email template for verifying OTP.
    /// Expected parameters:
    /// 0 - Full Name
    /// 1 - OTP Code
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static string VerifyOtp(string[] parameters)
    {
        // Định nghĩa parameters
        string fullName = parameters[0];
        string otp = parameters[1];

        return @$"<!doctype html>
			<html lang=""en"">
				<head>
					<meta charset=""UTF-8"" />
					<title></title>
				</head>

				<body
					style="";
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
			</html>";
    }

    /// <summary>
    /// Email template for reset password OTP.
    /// Expected parameters:
    /// 0 - Full Name
    /// 1 - OTP Code
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static string ResetPasswordOtp(string[] parameters)
    {
        // Định nghĩa parameters
        string fullName = parameters[0];
        string otp = parameters[1];

        return @$"<!doctype html>
			<html lang=""en"">
				<head>
					<meta charset=""UTF-8"" />
					<title>Reset Password - Ekofy</title>
				</head>

				<body
					style="";
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
								We have received a password reset request for your Ekofy account. For security purposes,
								please use the following One-Time Password (OTP) to reset your password.
								<br />
								<b>Your Password Reset OTP code is:</b>
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
								<strong>This OTP is valid for 10 minutes.</strong>
								<br />
								<br />
								If you did not request a password reset, please disregard this message and ensure your account security. 
								Please ensure the confidentiality of your OTP and do not share it with anyone.<br />
								<strong>Do not forward or give this code to anyone.</strong>
								<br />
								<br />
								<strong>Thank you for using Ekofy.</strong>
								<br />
								<br />
								Best regards,
								<br />
								<strong>The Ekofy Team</strong>
							</p>
						</div>
					</div>
				</body>
			</html>";
    }

    /// <summary>
    /// Email template for password changed notification.
    /// Expected parameters:
    /// 0 - Full Name
    /// 1 - Email
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static string PasswordChangedNotification(string[] parameters)
    {
        // Định nghĩa parameters
        string fullName = parameters[0];
        string email = parameters[1];

        return @$"<!doctype html>
			<html lang=""en"">
				<head>
					<meta charset=""UTF-8"" />
					<title>Password Changed - Ekofy</title>
				</head>

				<body
					style="";
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
								We are writing to confirm that your password for the Ekofy account associated with 
								<strong>{email}</strong> has been successfully changed.
							</p>
							<p>
								<strong>If you made this change:</strong><br>
								Your account is now secured with your new password. You can continue using Ekofy with your updated credentials.
							</p>
							<p>
								<strong>If you did not make this change:</strong><br>
								Please contact our support team immediately at <strong>support@ekofy.com</strong> as your account may have been compromised.
								For your security, we recommend:
							</p>
							<ul>
								<li>Change your password immediately</li>
								<li>Review your account activity</li>
								<li>Enable two-factor authentication if available</li>
							</ul>
							<p style=""font-size: 0.9em"">
								<strong>Thank you for using Ekofy.</strong>
								<br />
								<br />
								Best regards,
								<br />
								<strong>The Ekofy Team</strong>
							</p>
						</div>
					</div>
				</body>
			</html>";
    }
}
