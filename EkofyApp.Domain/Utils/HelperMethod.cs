using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using System.Globalization;
using System.Text;
using TimeZoneConverter;

namespace EkofyApp.Domain.Utils;
public sealed class HelperMethod
{
    public static IEnumerable<long> GetValidBitratesEnumrable()
    {
        // Đơn vị kbps -> 128000 tương đương 128 kbps

        // Dùng cho convert to HLS
        IEnumerable<long> validBitrates = [128000, 256000, 320000];

        return validBitrates;
    }

    public static string[] GetValidBitratesArray()
    {
        return ["128kbps", "256kbps", "320kbps"];
    }

    #region Time Zone
    public static DateTime GetUtcPlus7Time()
    {
        #region Chỉ chạy được trên local nếu publish thì sẽ lỗi
        // Lấy thời gian UTC hiện tại
        //DateTime utcNow = DateTime.UtcNow;

        //// Định nghĩa time zone UTC+7
        //TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        //// Chuyển đổi thời gian UTC thành UTC+7
        //DateTime utcPlus7Now = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
        #endregion

        // Lấy thời gian UTC hiện tại và cộng thêm offset 7 giờ
        DateTime utcPlus7Now = DateTime.UtcNow.AddHours(7);

        return utcPlus7Now;
    }

    public static DateOnly GetUtcPlus7DateOnly()
    {
        // Lấy thời gian UTC hiện tại và cộng thêm offset 7 giờ
        DateTime utcPlus7Now = DateTime.UtcNow.AddHours(7);
        // Trả về phần DateOnly của thời gian UTC+7
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

        // Kiểm tra xem ngày sinh có ở quá khứ không
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
    public static string RegexPatternPhoneNumber() => @"^(0|\+84)(32|33|34|35|36|37|38|39|86|96|97|98|81|82|83|84|85|88|91|94|70|76|77|78|79|89|90|93|52|56|58|92|059|099|095)[0-9]{7}$"; // Cái gì vậy trời ơi

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
