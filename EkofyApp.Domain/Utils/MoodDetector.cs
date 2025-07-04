using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.Utils;
public class MoodDetector
{
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
}
