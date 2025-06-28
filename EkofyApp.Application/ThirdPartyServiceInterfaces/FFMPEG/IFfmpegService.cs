using EkofyApp.Application.Models.Wavs;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
public interface IFfmpegService
{
    Task<string> ConvertToHls(string audioFilePath, string trackId, AudioConvertPathOptions audioConvertPathOptions);
    Task<WavFileResponse> ConvertToWavFileAsync(Stream inputStreamFile, string inputFileName, AudioConvertPathOptions audioConvertPathOptions);
}
