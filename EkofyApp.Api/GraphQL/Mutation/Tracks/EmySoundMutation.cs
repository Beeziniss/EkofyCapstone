using EkofyApp.Application.ThirdPartyServiceInterfaces.EmySound;
using EkofyApp.Domain.Enums;
using Refit;
using System.Text.Json;

namespace EkofyApp.Api.GraphQL.Mutation.Tracks;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class EmySoundMutation(IEmySoundApi emySoundApi)
{
    private readonly IEmySoundApi _emySoundApi = emySoundApi;

    public async Task<string> UploadTrackFingerprintAsync(IFile file, string trackId, string trackName, string artistName, string artistId)
    {
        using Stream stream = file.OpenReadStream();

        StreamPart streamPart = new(stream, file.Name, file.ContentType);
        string response = await _emySoundApi.UploadTrackAsync(streamPart, MediaType.Audio.ToString(), trackId, trackName, artistName, artistId);

        return response;
    }
}
