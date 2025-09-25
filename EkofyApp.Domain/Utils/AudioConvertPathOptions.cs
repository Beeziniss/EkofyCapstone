using EkofyApp.Domain.Exceptions;
using MongoDB.Bson;

namespace EkofyApp.Domain.Utils;
public sealed class AudioConvertPathOptions
{
    public string BasePath { get; set; } = string.Empty; // Mặc định
    public string RootFolder { get; set; } = string.Empty;
    public string InputIntermediateFolder { get; set; } = string.Empty;
    public string OutputIntermediateFolder { get; set; } = string.Empty;
    public string TargetFolder { get; set; } = string.Empty;

    public string CreateInputFolder()
    {
        string inputFolderPath = Path.Combine(BasePath, RootFolder, InputIntermediateFolder);
        if (!Directory.Exists(inputFolderPath))
        {
            Directory.CreateDirectory(inputFolderPath);
        }

        return inputFolderPath;
    }

    public string CreateOutputFolder()
    {
        string outputFolderPath = Path.Combine(BasePath, RootFolder, OutputIntermediateFolder);
        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }

        return outputFolderPath;
    }

    public string CreateTargetFolder()
    {
        if (string.IsNullOrEmpty(TargetFolder))
        {
            throw new ValidationCustomException("Target folder cannot be null or empty");
        }

        string targetFolderPath = Path.Combine(BasePath, RootFolder, OutputIntermediateFolder, TargetFolder);

        if (!Directory.Exists(targetFolderPath))
        {
            Directory.CreateDirectory(targetFolderPath);
        }

        return targetFolderPath;
    }

    public string CreateKeyFolder()
    {
        if (string.IsNullOrEmpty(TargetFolder))
        {
            throw new ValidationCustomException("Target folder cannot be null or empty");
        }

        string keyFolderPath = Path.Combine(BasePath, RootFolder, OutputIntermediateFolder, TargetFolder, "key");

        if (!Directory.Exists(keyFolderPath))
        {
            Directory.CreateDirectory(keyFolderPath);
        }
        return keyFolderPath;
    }

    public string CreateSegmentFolder(string bitrate)
    {
        if (string.IsNullOrEmpty(TargetFolder))
        {
            throw new ValidationCustomException("Target folder cannot be null or empty");
        }
        if (string.IsNullOrEmpty(bitrate))
        {
            throw new ValidationCustomException("Bitrate cannot be null or empty");
        }

        string segmentFolderPath = Path.Combine(BasePath, RootFolder, OutputIntermediateFolder, TargetFolder, bitrate);

        if (!Directory.Exists(segmentFolderPath))
        {
            Directory.CreateDirectory(segmentFolderPath);
        }

        return segmentFolderPath;
    }

    // Factory static methods
    public static AudioConvertPathOptions ForConvertToWav()
    {
        if (HelperMethod.IsWindows())
        {
            return new AudioConvertPathOptions
            {
                BasePath = AppDomain.CurrentDomain.BaseDirectory,
                RootFolder = "audio_processing",
                InputIntermediateFolder = "input_temp_audio",
                OutputIntermediateFolder = "output_wav_audio"
            };
        }
        else if (HelperMethod.IsLinux())
        {
            return new AudioConvertPathOptions
            {
                BasePath = "/app/shared",
                RootFolder = "audio_processing",
                InputIntermediateFolder = "input_temp_audio",
                OutputIntermediateFolder = "output_wav_audio"
            };
        }
        else
        {
            throw new ValidationCustomException("Unsupported OS platform");
        }
    }

    public static AudioConvertPathOptions ForConvertToHls(string trackId)
    {
        if (string.IsNullOrEmpty(trackId))
        {
            throw new ValidationCustomException("Track id cannot be null or empty");
        }

        if (HelperMethod.IsWindows())
        {
            return new AudioConvertPathOptions
            {
                BasePath = AppDomain.CurrentDomain.BaseDirectory,
                RootFolder = "audio_processing",
                InputIntermediateFolder = "input_temp_audio",
                OutputIntermediateFolder = "output_hls_audio",
                TargetFolder = trackId,
            };
        }
        else if (HelperMethod.IsLinux())
        {
            return new AudioConvertPathOptions
            {
                BasePath = "/app/shared",
                RootFolder = "audio_processing",
                InputIntermediateFolder = "input_temp_audio",
                OutputIntermediateFolder = "output_hls_audio",
                TargetFolder = trackId,
            };
        }
        else
        {
            throw new ValidationCustomException("Unsupported OS platform");
        }

    }

    // Optional: Có thể custom
    public static AudioConvertPathOptions CreateCustom(
        string root,
        string? input,
        string output,
        string? basePath = null,
        string? target = null)
    {
        return new AudioConvertPathOptions
        {
            BasePath = basePath ?? AppDomain.CurrentDomain.BaseDirectory, // Sẽ xem xét dùng temp path nếu không có basePath
            RootFolder = root,
            InputIntermediateFolder = input ?? string.Empty,
            OutputIntermediateFolder = output,
            TargetFolder = target ?? ObjectId.GenerateNewId().ToString() // Tạo ID mới nếu không có target
        };
    }
}

