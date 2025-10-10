using Refit;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.EmySound;
public interface IEmySoundApi
{
    const string Query = "api/v1.1/Query";
    const string Tracks = "api/v1.1/Tracks";
    const string MediaTypeAudio = "Audio";

    [Multipart]
    [Post($"/{Query}")]
    Task<HttpResponseMessage> QueryTrackAsync(
        [AliasAs("file")] StreamPart file,
        [AliasAs("mediaType")] string mediaType,
        [AliasAs("minConfidence")] double minConfidence,
        [AliasAs("minCoverage")] double minCoverage,
        [AliasAs("registerMatches")] bool registerMatches = true
    );

    [Multipart]
    [Post($"/{Tracks}")]
    Task<string> UploadTrackAsync(
        [AliasAs("file")] StreamPart file,
        [AliasAs("MediaType")] string mediaType,
        [AliasAs("UserId")] string trackId,
        [AliasAs("Title")] string trackName,
        [AliasAs("Artist")] string artistId
    );

}
