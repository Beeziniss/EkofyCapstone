using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TimeZoneConverter;

namespace EkofyApp.Domain.Utils;

public sealed class HelperMethod
{
    public static string? ValidateAndCombineName(string? firstName, string? lastName)
    {
        // Nếu cả 2 biến đều là null thì trả về null thay vì chuỗi rỗng
        if (firstName == null && lastName == null)
        {
            return null;
        }

        // Cắt bỏ khoảng trắng ở đầu và cuối của firstName và lastName
        string trimmedFirstName = firstName?.Trim() ?? string.Empty;
        string trimmedLastName = lastName?.Trim() ?? string.Empty;

        // Kết hợp firstName và lastName với một khoảng trắng ở giữa
        string fullName = $"{trimmedLastName} {trimmedFirstName}".Trim();

        return fullName;
    }

    public static string NormalizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Biểu thức chính quy tìm kiếm:
        // (?<=[a-z]) : Positive Lookbehind - Đảm bảo trước đó là một ký tự thường (a-z)
        // (?=[A-Z]) : Positive Lookahead - Đảm bảo sau đó là một ký tự hoa (A-Z)
        // Kết hợp lại để tìm vị trí *giữa* một ký tự thường và một ký tự hoa, sau đó chèn " " (dấu cách).
        return Regex.Replace(input, "(?<=[a-z])(?=[A-Z])", " ");
    }

    public static string BuildContentNotification(NotificationActionType action, NotificationRelatedType? relatedType, string? relatedName, string actorName)
    {
        return (action, relatedType) switch
        {
            // FOLLOW
            (NotificationActionType.Follow, null)
            => $"{actorName} started following you.",

            // Release
            (NotificationActionType.Release, NotificationRelatedType.Track)
                => $"{actorName} released a new track \"{relatedName ?? "Unknown"}\".",

            (NotificationActionType.Release, NotificationRelatedType.Album)
                => $"{actorName} released a new album \"{relatedName ?? "Unknown"}\".",

            // LIKE
            (NotificationActionType.Like, NotificationRelatedType.Track)
                => $"{actorName} liked your track \"{relatedName ?? "Unknown"}\".",

            (NotificationActionType.Like, NotificationRelatedType.Album)
                => $"{actorName} liked your album \"{relatedName ?? "Unknown"}\".",

            (NotificationActionType.Like, NotificationRelatedType.Playlist)
                => $"{actorName} liked your playlist.",

            // COMMENT
            (NotificationActionType.Comment, NotificationRelatedType.Track)
                => $"{actorName} commented on your track \"{relatedName ?? "Unknown"}\".",

            (NotificationActionType.Comment, NotificationRelatedType.Request)
            => $"{actorName} commented on your request \"{relatedName ?? "Unknown"}\".",

            (NotificationActionType.Comment, NotificationRelatedType.Comment)
                => $"{actorName} replied to your comment.",

            // REVIEW
            (NotificationActionType.Review, NotificationRelatedType.Review)
                => $"{actorName} reviewed your service at #{relatedName ?? "Unknown"}.",

            // ORDER
            (NotificationActionType.OrderCompleted, NotificationRelatedType.Order)
                => $"Your order #{relatedName ?? "N/A"} has been completed.",

            (NotificationActionType.OrderDeadline, NotificationRelatedType.Order)
                => $"Your order with user {actorName} must be finish in 24 hours.",

            (NotificationActionType.OrderCreated, NotificationRelatedType.Order)
                => $"{actorName} apccepted your order.",

            (NotificationActionType.OrderContinued, NotificationRelatedType.Order)
                => $"Your order #{relatedName ?? "N/A"} turned from disputed into in progress. Please continue your work",

            (NotificationActionType.OrderDisputed, NotificationRelatedType.Order)
                => $"Your order #{relatedName ?? "N/A"} has been disputed.",

            (NotificationActionType.OrderRefunded, NotificationRelatedType.Order)
                => $"Your order #{relatedName ?? "N/A"} has been refunded.",

            // REQUEST
            (NotificationActionType.RequestCreated, NotificationRelatedType.Request)
                => $"{actorName} submitted a new request.",

            (NotificationActionType.RequestApproved, NotificationRelatedType.Request)
                => "Your request has been approved.",

            (NotificationActionType.RequestRejected, NotificationRelatedType.Request)
                => "Your request has been rejected.",

            _ => "You have a new notification."
        };
    }

    public static IEnumerable<long> GetValidBitratesEnumrable()
    {
        // Đơn vị kbps -> 128000 tương đương 128 kbps

        // Dùng cho convert to HLS
        IEnumerable<long> validBitrates = [128000, 256000, 320000];

        return validBitrates;
    }

    public static string[] GetAllowedBitratesForUser(List<AppliedEntitlement> appliedEntitlements)
    {
        string maxBitrate = Convert.ToString(appliedEntitlements.Where(x => x.Code == "audio_high_quality").Select(x => x.Value).FirstOrDefault()) ?? throw new NotFoundCustomException("Not found entitlement code audio_high_quality");

        string maxBitrateDefault = "128kbps"; // Mặc định

        if (maxBitrate == maxBitrateDefault)
        {
            return ["128kbps"];
        }

        return ["128kbps", "256kbps", "320kbps"];
    }

    #region Time Zone
    public static DateTime GetUtcPlus7Time()
    {
        #region Chỉ chạy được trên local nếu publish thì sẽ lỗi
        // Get the current UTC time
        //DateTime utcNow = DateTime.UtcNow;

        //// Define the UTC+7 time zone
        //TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        //// Convert the UTC time to UTC+7
        //DateTime utcPlus7Now = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
        #endregion

        // Get the current UTC time and add a 7-hour offset
        DateTime utcPlus7Now = DateTime.UtcNow.AddHours(7);

        return utcPlus7Now;
    }

    public static DateOnly GetUtcPlus7DateOnly()
    {
        // Get the current UTC time and add a 7-hour offset
        DateTime utcPlus7Now = DateTime.UtcNow.AddHours(7);
        // Return the DateOnly part of the UTC+7 time
        return DateOnly.FromDateTime(utcPlus7Now);
    }

    public static DateTimeOffset ConvertDateTimeToUtcPlus7TimeOffset(DateTime dateTime)
    {
        // IANA TimeZone (dùng chung cho mọi nền tảng)
        string ianaTimeZone = "Asia/Ho_Chi_Minh";

        // Chuyển đổi sang Windows TimeZone (dùng được cho cả Windows & Linux)
        string windowsTimeZone = TZConvert.IanaToWindows(ianaTimeZone);

        // Lấy TimeZoneInfo phù hợp
        TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZone);

        // Chuyển đổi UTC → Local time
        return TimeZoneInfo.ConvertTimeFromUtc(dateTime, tzi);
    }

    public static DateTimeOffset GetUtcPlus7TimeOffset()
    {
        return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));
    }

    public static DateTimeOffset GetUtcTimeOffset(int hoursOffset)
    {
        // Tính toán thời gian với offset
        return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(hoursOffset));
    }

    public static DateTimeOffset GetServerTimeOffset()
    {
        // Lấy thời gian hiện tại của server (UTC+7)
        return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));
    }

    /// <summary>
    /// Dùng cho kiểm soát đầu vào time của dữ liệu từ bên ngoài
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static DateTimeOffset NormalizeToUtcPlus7TimeOffset(DateTimeOffset dateTime)
    {
        string tzId = TZConvert.IanaToWindows("Asia/Ho_Chi_Minh");
        TimeZoneInfo tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        return TimeZoneInfo.ConvertTime(dateTime, tzInfo);
    }

    public static string NormalizeToStringUtcPlus7(DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.ToString("dd-MM-yyyy HH:mm:ss");
    }

    public static DateTimeOffset ParseFromStringUtcPlus7(string dateTimeString)
    {
        if (string.IsNullOrWhiteSpace(dateTimeString))
        {
            throw new ArgumentException("Date time string cannot be null or empty.", nameof(dateTimeString));
        }

        if (!DateTimeOffset.TryParseExact(dateTimeString, "dd-MM-yyyy HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset result))
        {
            throw new FormatException($"Invalid date time format. Expected format: dd-MM-yyyy HH:mm:ss. Actual: {dateTimeString}");
        }

        // Apply UTC+7 offset to match the normalize method
        return new DateTimeOffset(result.DateTime, TimeSpan.FromHours(7));
    }
    #endregion

    #region Operation System Handle
    public static bool IsWindows()
    {
        return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
    }

    public static bool IsLinux()
    {
        return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
    }
    #endregion

    #region Resolve Path Tags
    public static string ResolvePath(PathTag tag, params string[] more)
    {
        string baseDir = AppContext.BaseDirectory;

        string result = tag switch
        {
            PathTag.Bin => baseDir,
            PathTag.Base => Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..")),
            PathTag.Api => Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..")),
            PathTag.Tools => Path.GetFullPath(Path.Combine(Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..")), "Tools")),
            PathTag.PrivateKeys => Path.GetFullPath(Path.Combine(Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..")), "PrivateKeys")),
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
        };

        if (more != null && more.Length > 0)
        {
            result = Path.Combine(new[] { result }.Concat(more).ToArray());
        }

        return result;
    }
    #endregion

    #region Delete Directories and Files
    public static void DeleteBatchIO(params string[] paths)
    {
        foreach (string path in paths)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
    #endregion

    #region Calculate Identity Card Expiry
    [Obsolete("Wrong data type (instead of DateTimeOffset).")]
    public static DateTime CalculateIdentityCardExpiry(DateTime dateOfBirth)
    {
        // Input phải là 7+ Time Zone
        if (dateOfBirth.Kind != DateTimeKind.Utc)
        {
            throw new BadRequestCustomException("Date of birth must be in UTC+7 time zone.");
        }

        DateTime now = DateTime.UtcNow.AddHours(7);

        // Validate that the date of birth is in the past
        if (dateOfBirth > now)
        {
            throw new BadRequestCustomException("Date of birth must be in the past.");
        }

        int age = now.Year - dateOfBirth.Year;
        if (now < dateOfBirth.AddYears(age))
        {
            age--;
        }

        return age switch
        {
            < 25 => dateOfBirth.AddYears(25),
            < 40 => dateOfBirth.AddYears(40),
            < 60 => dateOfBirth.AddYears(60),
            _ => DateTime.MaxValue
        };
    }
    #endregion

    #region Calculate Age
    [Obsolete("Use GetExactAge(DateTimeOffset) instead.")]
    public static int GetExactAge(DateTime birthDate)
    {
        DateTime today = GetUtcPlus7Time().Date;

        int age = today.Year - birthDate.Year;
        if (today < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }

    public static int GetExactAge(DateTimeOffset birthDate)
    {
        // Chuyển ngày hiện tại về giờ VN (UTC+7)
        DateTimeOffset today = DateTime.UtcNow.AddHours(7).Date;

        // Chuyển ngày sinh về giờ UTC+7 để so sánh đúng (nếu cần)
        DateTimeOffset birthLocal = birthDate.ToOffset(today.Offset).Date;

        int age = today.Year - birthLocal.Year;

        if (today < birthLocal.AddYears(age))
        {
            age--;
        }

        return age;
    }

    #endregion

    #region Regex Validation
    public static string RegexPatternAlpha() => @"^[\p{L}]+$";
    public static string RegexPatternAlphaWithSpace() => @"^[\p{L} ]+$";
    public static string RegexPatternAlphanumeric() => @"^[\p{L}0-9]+$";
    public static string RegexPatternAlphaNumericWithSpace() => @"^[\p{L}0-9 ]+$";
    public static string RegexPatternAlphaNumericWithSpecific() => @"^[\p{L}0-9 ,./\-_]+$";
    public static string RegexPatternIdentityCardNumber() => @"^\d{9}|\d{12}$";
    public static string RegexPatternPhoneNumber() => @"^(0|\+84)(32|33|34|35|36|37|38|39|86|96|97|98|81|82|83|84|85|88|91|94|70|76|77|78|79|89|90|93|52|56|58|92|059|099|095)[0-9]{7}$"; // WTF IS THIS COPILOT

    // 32|33|34|35|36|37|38|39|86|96|97|98 |  # Viettel
    // 81|82|83|84|85|88|91|94 |              # Vinaphone
    // 70|76|77|78|79|89|90|93 |              # Mobifone
    // 52|56|58|92 |                          # Vietnamobile
    // 059|099 |                              # Beeline/Gmobile
    // 095                                   # S‑Fone (WTF)
    #endregion

    #region Normalize Strings
    // Giữ lại hàm ToUnsigned để chuẩn hóa input của người dùng
    public static string ToUnsigned(string term)
    {
        if (string.IsNullOrEmpty(term))
        {
            return string.Empty;
        }

        string normalizedString = term.Normalize(NormalizationForm.FormD);

        StringBuilder stringBuilder = new();
        foreach (char character in normalizedString)
        {
            UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(character);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Replace('đ', 'd');
    }
    #endregion
}
