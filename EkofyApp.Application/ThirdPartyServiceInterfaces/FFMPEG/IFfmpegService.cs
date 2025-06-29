using EkofyApp.Application.Models.Wavs;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
public interface IFfmpegService
{
    Task<string> ConvertToHls(WavFileResponse wavFileResponse, AudioConvertPathOptions audioConvertPathOptions);
    Task<WavFileResponse> ConvertToWavFileAsync(Stream inputStreamFile, string inputFileName, AudioConvertPathOptions audioConvertPathOptions);
}
