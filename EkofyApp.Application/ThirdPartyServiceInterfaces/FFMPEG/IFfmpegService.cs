using EkofyApp.Application.Models.Wavs;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
public interface IFfmpegService
{
    Task<string> ConvertToHlsAsync(WavFileResponse wavFileResponse, AudioConvertPathOptions audioConvertPathOptions);
    Task<WavFileResponse> ConvertToWavAsync(Stream inputStreamFile, string inputFileName, AudioConvertPathOptions audioConvertPathOptions);
}
