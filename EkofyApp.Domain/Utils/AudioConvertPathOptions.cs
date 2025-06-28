using MongoDB.Bson;

namespace EkofyApp.Domain.Utils;
public class AudioConvertPathOptions
{
    public string BasePath { get; set; } = string.Empty; // Mặc định
    public string RootFolder { get; set; } = string.Empty;
    public string InputIntermediateFolder { get; set; } = string.Empty;
    public string OutputIntermediateFolder { get; set; } = string.Empty;
    public string TargetFolder { get; set; } = string.Empty;

    // Dùng cho convert to HLS
    public IEnumerable<long> Bitrates = []; // Các bitrate có thể sử dụng

    public string GetInputFolder()
        => Path.Combine(BasePath, RootFolder, InputIntermediateFolder);

    public string GetOutputFolder()
        => Path.Combine(BasePath, RootFolder, OutputIntermediateFolder);

    // Factory static methods
    public static AudioConvertPathOptions ForConvertToWav()
    {
        return new AudioConvertPathOptions
        {
            BasePath = AppDomain.CurrentDomain.BaseDirectory,
            RootFolder = "audio_processing",
            InputIntermediateFolder = "input_temp_audio",
            OutputIntermediateFolder = "output_wav_audio"
        };
    }

    public static AudioConvertPathOptions ForConvertToHls()
    {
        return new AudioConvertPathOptions
        {
            BasePath = AppDomain.CurrentDomain.BaseDirectory,
            RootFolder = "audio_processing",
            InputIntermediateFolder = "input_temp_audio",
            OutputIntermediateFolder = "output_hls_audio",
            TargetFolder = ObjectId.GenerateNewId().ToString(),

            Bitrates = [128000, 256000, 320000]
        };
    }

    // Optional: Có thể custom
    public static AudioConvertPathOptions CreateCustom(
        string root,
        string input,
        string output,
        string? basePath = null)
    {
        return new AudioConvertPathOptions
        {
            BasePath = basePath ?? AppDomain.CurrentDomain.BaseDirectory, // Sẽ xem xét dùng temp path nếu không có basePath
            RootFolder = root,
            InputIntermediateFolder = input,
            OutputIntermediateFolder = output
        };
    }
}

