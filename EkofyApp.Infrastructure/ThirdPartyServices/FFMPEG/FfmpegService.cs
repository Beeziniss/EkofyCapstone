using EkofyApp.Application.Models.Wavs;
using EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using MongoDB.Bson;
using Xabe.FFmpeg;

namespace EkofyApp.Infrastructure.ThirdPartyServices.FFMPEG;
public class FfmpegService : IFfmpegService
{
    private readonly string _ffmpegPath;

    public FfmpegService()
    {
        _ffmpegPath = PathHelper.ResolvePath(PathTag.Base, "Tools");
        _ffmpegPath = Path.GetFullPath(_ffmpegPath);

        FFmpeg.SetExecutablesPath(_ffmpegPath);

        if (!File.Exists(Path.Combine(_ffmpegPath, "ffmpeg.exe")) &&
            !File.Exists(Path.Combine(_ffmpegPath, "ffprobe.exe")))
        {
            throw new FileNotFoundException($"FFmpeg not found in path: {_ffmpegPath}");
        }
    }

    // Convert IFormFile to Waveform Audio File
    public async Task<WavFileResponse> ConvertToWavFileAsync(Stream inputStreamFile, string inputFileName, AudioConvertPathOptions audioConvertPathOptions)
    {
        if (inputStreamFile == null || inputStreamFile.Length == 0)
            throw new ArgumentException("Tệp âm thanh không hợp lệ.");

        // Tạo file tạm input (mp3, m4a...)
        string inputFileExtension = Path.GetExtension(inputFileName);

        //string inputTempPath = Path.Combine(Path.GetTempPath(), $"{ObjectId.GenerateNewId()}{inputFileExtension}");

        audioConvertPathOptions.BasePath ??= Path.GetTempPath(); // Nếu basePath null thì dùng thư mục tạm hệ thống
        audioConvertPathOptions.RootFolder ??= string.Empty;
        audioConvertPathOptions.InputIntermediateFolder ??= string.Empty;
        audioConvertPathOptions.OutputIntermediateFolder ??= string.Empty;
        //string inputFileName = Path.GetFileNameWithoutExtension(inputStreamFile.FileName);

        // Nếu basePath, rootFolder, intermediateFolder không null thì tạo đường dẫn tạm theo cấu trúc
        string inputFolderTempPath = Path.Combine(audioConvertPathOptions.BasePath, audioConvertPathOptions.RootFolder, audioConvertPathOptions.InputIntermediateFolder);
        string outputFolderTempPath = Path.Combine(audioConvertPathOptions.BasePath, audioConvertPathOptions.RootFolder, audioConvertPathOptions.OutputIntermediateFolder);

        string outputWavPath = string.Empty;
        long bitrate = default;

        // Tạo thư mục nếu chưa tồn tại
        if (!Directory.Exists(inputFolderTempPath))
        {
            Directory.CreateDirectory(inputFolderTempPath);
        }
        if (!Directory.Exists(outputFolderTempPath))
        {
            Directory.CreateDirectory(outputFolderTempPath);
        }

        try
        {
            string inputTempPath = Path.Combine(inputFolderTempPath, $"{ObjectId.GenerateNewId()}_{inputFileName}{inputFileExtension}");
            using (var stream = new FileStream(inputTempPath, FileMode.Create))
            {
                await inputStreamFile.CopyToAsync(stream);
            }

            // Tạo đường dẫn file .wav tạm
            outputWavPath = Path.Combine(outputFolderTempPath, $"{ObjectId.GenerateNewId()}.wav");

            // Kiểm tra file đầu vào có hợp lệ không
            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputTempPath);
            if (!mediaInfo.AudioStreams.Any())
                throw new InvalidOperationException("Tệp âm thanh không chứa stream âm thanh hợp lệ.");

            // Lấy stream âm thanh đầu tiên (nếu có nhiều stream thì lấy stream đầu tiên)
            IAudioStream? audioStream = mediaInfo.AudioStreams.FirstOrDefault() ?? throw new ArgumentNullException("Audio Stream is null");

            // Nếu không có bitrate thì dùng 128k
            bitrate = audioStream.Bitrate;

            // Convert dùng Xabe.FFmpeg
            //IConversion conversion = await FFmpeg.Conversions.FromSnippet.Convert(inputTempPath, outputWavPath);
            //conversion.AddParameter("-ac 1 -ar 16000"); // Mono, 16kHz nếu cần
            IConversion conversion = FFmpeg.Conversions.New()
                .AddStream(audioStream)
                .SetOutput(outputWavPath);

            await conversion.Start();

            // Xoá input tạm
            if (File.Exists(inputTempPath))
            {
                File.Delete(inputTempPath);
            }
        }
        catch
        {
            // Xoá thư mục tạm nếu có lỗi xảy ra
            if (Directory.Exists(inputFolderTempPath))
            {
                Directory.Delete(inputFolderTempPath, true); // Xóa cả file bên trong
            }
            if (Directory.Exists(outputFolderTempPath))
            {
                Directory.Delete(outputFolderTempPath, true); // Xóa cả file bên trong
            }
        }

        return new WavFileResponse()
        {
            OutputWavPath = outputWavPath,
            Bitrate = bitrate
        };
    }

    public async Task<string> ConvertToHls(string audioFilePath, string trackId, AudioConvertPathOptions audioConvertPathOptions)
    {
        List<(long bitrate, string relativePath)> playlistEntries = [];

        string outputFolder = string.Empty;
        string outputFilePath = string.Empty;
        string bitrateVersionFolder = string.Empty;

        audioConvertPathOptions.BasePath ??= Path.GetTempPath(); // Nếu basePath null thì dùng thư mục tạm hệ thống
        audioConvertPathOptions.RootFolder ??= string.Empty;
        audioConvertPathOptions.OutputIntermediateFolder ??= string.Empty;
        audioConvertPathOptions.TargetFolder ??= ObjectId.GenerateNewId().ToString();

        string targetRootFolder = Path.Combine(audioConvertPathOptions.BasePath, audioConvertPathOptions.RootFolder, audioConvertPathOptions.OutputIntermediateFolder, audioConvertPathOptions.TargetFolder);

        string keyDirectory = string.Empty;

        try
        {
            string playlistFileName = $"{trackId}_hls.m3u8";

            // Lấy key và iv từ environment để mã hóa
            string keyHex = Environment.GetEnvironmentVariable("HLS_KEY")!;
            string ivHex = Environment.GetEnvironmentVariable("HLS_IV")!;
            //string keyUriTemplate = Environment.GetEnvironmentVariable("HLS_KEY_URL")!;
            //string keyUri = keyUriTemplate.Replace("{trackId}", trackId); // Dùng API để lấy key theo track
            string keyUriHidden = Environment.GetEnvironmentVariable("HLS_KEY_URL_HIDDEN")!;

            // Tạo folder key để mã hóa HLS
            keyDirectory = Path.Combine(targetRootFolder, "key");
            if(!Directory.Exists(keyDirectory))
            {
                Directory.CreateDirectory(keyDirectory); // Nhớ xóa folder này sau khi sử dụng
            }
            
            string keyFilePath = Path.Combine(keyDirectory, "encryption.key");
            string keyInfoPath = Path.Combine(keyDirectory, "key_info.txt");

            // Ghi key file và key info file
            await File.WriteAllBytesAsync(keyFilePath, Convert.FromHexString(keyHex));
            await File.WriteAllLinesAsync(keyInfoPath,
            [
                //keyUri,
                keyUriHidden, // Sử dụng keyUriHidden để ẩn URI
                keyFilePath,
                ivHex
            ]);

            // Kiểm tra file đầu vào có hợp lệ không
            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(audioFilePath);
            //if (!mediaInfo.AudioStreams.Any())
            //    throw new InvalidOperationException("AudioFile không chứa stream âm thanh hợp lệ.");

            IAudioStream? audioStream = mediaInfo.AudioStreams.FirstOrDefault() ?? throw new ArgumentNullCustomException("Audio Stream is null");
            long bitrate = audioStream.Bitrate;

            foreach (long bitrateIndex in audioConvertPathOptions.Bitrates)
            {
                if (bitrateIndex > bitrate)
                {
                    return targetRootFolder;
                }

                string bitrateDisplay = (bitrateIndex / 1000).ToString("D3") + "kbps";
                outputFolder = Path.Combine(audioConvertPathOptions.BasePath, audioConvertPathOptions.RootFolder, audioConvertPathOptions.OutputIntermediateFolder, $"{audioConvertPathOptions.TargetFolder}", $"{bitrateDisplay}");

                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                // Cấp quyền cho thư mục
                //Syscall.chmod(inputFolder, FilePermissions.ALLPERMS);
                //Syscall.chmod(outputFolder, FilePermissions.ALLPERMS);

                outputFilePath = Path.Combine(outputFolder, playlistFileName);

                // Chuyển đổi bằng cách thêm Stream thay vì AddParameter
                await FFmpeg.Conversions.New()
                    .AddStream(audioStream) // Lấy stream âm thanh
                    .SetOutput(outputFilePath)
                    .AddParameter($"-c:a aac -b:a {bitrateIndex} -hls_time 10 -hls_playlist_type vod -hls_key_info_file \"{keyInfoPath}\"")
                    .Start();

                // Ghi lại relative path để thêm vào master playlist
                string relativePath = $"{bitrateDisplay}/{playlistFileName}";
                playlistEntries.Add((bitrateIndex, relativePath));
            }

            // Tạo master.m3u8 ở folder gốc (targetFolder)
            string masterFilePath = Path.Combine(targetRootFolder, $"{audioConvertPathOptions.TargetFolder}_master.m3u8");
            List<string> lines = ["#EXTM3U"];
            foreach ((long bitrate, string relativePath) entry in playlistEntries.OrderBy(e => e.bitrate))
            {
                lines.Add($"#EXT-X-STREAM-INF:BANDWIDTH={entry.bitrate}");
                lines.Add(entry.relativePath);
            }

            await File.WriteAllLinesAsync(masterFilePath, lines);
        }
        catch
        {
            if (Directory.Exists(audioFilePath))
            {
                Directory.Delete(audioFilePath, true); // Xóa cả file bên trong
            }

            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, true); // Xóa cả file bên trong
            }
        }
        finally
        {
            // Xóa file WAV input nếu tồn tại
            if (File.Exists(audioFilePath))
            {
                File.Delete(audioFilePath);
            }

            // Xóa folder key sau khi sử dụng
            if (Directory.Exists(keyDirectory))
            {
                Directory.Delete(keyDirectory, true); 
            }
        }

        return targetRootFolder;
    }
}
