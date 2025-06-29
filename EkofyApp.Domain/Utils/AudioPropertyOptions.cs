namespace EkofyApp.Domain.Utils;
public class AudioPropertyOptions
{
    public int Bitrate { get; private set; }
    public int SampleRate { get; private set; }
    public int Channels { get; private set; }
    public string Format { get; private set; }

    public static AudioPropertyOptions Default => new()
    {
        Bitrate = 128, // Mặc định 128 kbps
        SampleRate = 44100, // Mặc định 44.1 kHz
        Channels = 2, // Mặc định Stereo
        Format = "mp3", // Mặc định mp3
    };
}
