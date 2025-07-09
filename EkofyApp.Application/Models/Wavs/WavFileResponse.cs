namespace EkofyApp.Application.Models.Wavs;
public record WavFileResponse
{
    public string OutputWavPath { get; set; } = default!;
    public long OriginalBitrate { get; set; } = default!;
}
