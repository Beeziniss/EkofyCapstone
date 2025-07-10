using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.Utils;
public sealed class HelperMethod
{
    public static IEnumerable<long> GetValidBitrates()
    {
        // Đơn vị kbps -> 128000 tương đương 128 kbps

        // Dùng cho convert to HLS
        IEnumerable<long> validBitrates = [128000, 256000, 320000];

        return validBitrates;
    }

    #region UTC+7 Time Zone
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

    #region Mood Detection
    public static IEnumerable<MoodType> DetectMoods(AudioFeature feature)
    {
        List<MoodType> moods = [];

        // Happy
        if (feature.Tempo > 110 &&
            feature.ModeNumber == 1 &&
            feature.Energy > 0.5 &&
            feature.Danceability > 0.5)
        {
            moods.Add(MoodType.Happy);
        }

        // Calm
        if (feature.Tempo < 90 &&
            feature.Acousticness > 0.5 &&
            feature.Energy < 0.3 &&
            feature.ZeroCrossingRate < 0.05)
        {
            moods.Add(MoodType.Calm);
        }

        // Sad
        if (feature.Tempo < 85 &&
            feature.ModeNumber == 0 &&
            feature.Energy < 0.3 &&
            feature.SpectralCentroid < 2500 &&
            IsMfccLow(feature.MfccMean))
        {
            moods.Add(MoodType.Sad);
        }

        // Angry
        if (feature.Energy > 0.7 &&
            feature.ZeroCrossingRate > 0.1 &&
            feature.SpectralCentroid > 4500 &&
            feature.ModeNumber == 0 &&
            IsMfccUnstable(feature.MfccMean))
        {
            moods.Add(MoodType.Angry);
        }

        // Relaxed
        if (feature.Tempo < 100 &&
            feature.Danceability > 0.5 &&
            feature.Acousticness > 0.4)
        {
            moods.Add(MoodType.Relaxed);
        }

        // Energetic
        if (feature.Tempo > 120 &&
            feature.Energy > 0.6 &&
            feature.Danceability > 0.6)
        {
            moods.Add(MoodType.Energetic);
        }

        // Dark
        if (feature.SpectralCentroid < 2000 &&
            feature.Energy < 0.4 &&
            feature.ModeNumber == 0)
        {
            moods.Add(MoodType.Dark);
        }

        // Romantic
        if (feature.Tempo >= 60 && feature.Tempo <= 90 &&
            feature.ChromaMean.Average() > 0.5 &&
            feature.Energy < 0.5)
        {
            moods.Add(MoodType.Romantic);
        }

        // Chill
        if (feature.Energy < 0.4 &&
            feature.Acousticness > 0.4 &&
            feature.Tempo < 100)
        {
            moods.Add(MoodType.Chill);
        }

        return moods.Distinct().ToList();
    }

    private static bool IsMfccLow(List<float> mfcc)
    {
        return mfcc.Count > 0 && mfcc.Average() < 0;
    }

    private static bool IsMfccUnstable(List<float> mfcc)
    {
        if (mfcc.Count < 2)
        {
            return false;
        }

        double stdDev = Math.Sqrt(mfcc.Average(x => Math.Pow(x - mfcc.Average(), 2)));
        return stdDev > 30;
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
}
