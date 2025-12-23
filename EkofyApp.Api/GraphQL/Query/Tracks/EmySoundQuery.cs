using EkofyApp.Application.Models.AudioFingerprints;
using EkofyApp.Application.ThirdPartyServiceInterfaces.EmySound;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Refit;
using System.Text.Json;

namespace EkofyApp.Api.GraphQL.Query.Tracks;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class EmySoundQuery(IEmySoundApi emySoundApi)
{
    private readonly IEmySoundApi _emySoundApi = emySoundApi;

    [AuthorizeRoles(HelperRoleBase.ListenerArtistRoles)]
    public async Task<IEnumerable<QueryAudioFingerprintResponse>> QueryTracksAsync(IFile file)
    {
        using Stream stream = file.OpenReadStream();

        StreamPart streamPart = new(stream, file.Name, file.ContentType);
        HttpResponseMessage response = await _emySoundApi.QueryTrackAsync(streamPart, MediaType.Audio.ToString(), 0.8, 0.6);

        string body = await response.Content.ReadAsStringAsync();

        // Parse chuỗi JSON → root là mảng
        JsonElement root = JsonDocument.Parse(body).RootElement;
        if (root.GetArrayLength() == 0)
        {
            //throw new NotFoundCustomException("No matching track found.");
            return [];
        }

        List<QueryAudioFingerprintResponse> results = [];
        foreach (JsonElement item in root.EnumerateArray())
        {
            // Lấy track fields
            JsonElement track = item.GetProperty("track");
            JsonElement audioCoverage = item.GetProperty("audio").GetProperty("coverage");
            results.Add(new QueryAudioFingerprintResponse
            {
                TrackId = track.GetProperty("id").GetString() ?? throw new NotFoundCustomException("Track UserId is empty."),
                TrackName = track.GetProperty("title").GetString() ?? throw new NotFoundCustomException("Track Name is empty."),
                ArtistName = track.GetProperty("artist").GetString() ?? throw new NotFoundCustomException("Artist Name is empty."),
                TrackMatchStartsAt = audioCoverage.GetProperty("trackMatchStartsAt").GetDouble(),
                TrackMatchEndsAt = audioCoverage.GetProperty("trackMatchStartsAt").GetDouble() + audioCoverage.GetProperty("trackCoverageLength").GetDouble(),
            });
        }

        return results;
    }

    [AuthorizeRoles(HelperRoleBase.ListenerArtistRoles)]
    public async Task<QueryAudioFingerprintResponse> QueryTrackAsync(IFile file)
    {
        using Stream stream = file.OpenReadStream();

        StreamPart streamPart = new(stream, file.Name, file.ContentType);
        HttpResponseMessage response = await _emySoundApi.QueryTrackAsync(streamPart, MediaType.Audio.ToString(), 0.8, 0.6);

        string body = await response.Content.ReadAsStringAsync();

        // Parse chuỗi JSON → root là mảng
        JsonElement root = JsonDocument.Parse(body).RootElement;

        if (root.GetArrayLength() == 0)
        {
            throw new NotFoundCustomException("No matching track found.");
        }

        // Lấy phần tử đầu tiên trong mảng
        JsonElement firstItem = root[0];

        // Lấy track fields
        JsonElement track = firstItem.GetProperty("track");
        JsonElement audioCoverage = firstItem.GetProperty("audio").GetProperty("coverage");

        return new QueryAudioFingerprintResponse
        {
            TrackId = track.GetProperty("id").GetString() ?? throw new NotFoundCustomException("Track UserId is empty."),
            TrackName = track.GetProperty("title").GetString() ?? throw new NotFoundCustomException("Track Name is empty."),
            ArtistName = track.GetProperty("artist").GetString() ?? throw new NotFoundCustomException("Artist Name is empty."),
            ArtistId = track.GetProperty("metaFields").GetProperty("UserId").GetString() ?? throw new NotFoundCustomException("Artist UserId is empty."),
            MediaType = track.GetProperty("mediaType").GetString() ?? throw new NotFoundCustomException("Media Type is empty."),

            QueryMatchStartsAt = audioCoverage.GetProperty("queryMatchStartsAt").GetDouble(),
            QueryMatchEndsAt = audioCoverage.GetProperty("queryMatchStartsAt").GetDouble() + audioCoverage.GetProperty("queryCoverageLength").GetDouble(),
            TrackMatchStartsAt = audioCoverage.GetProperty("trackMatchStartsAt").GetDouble(),
            TrackMatchEndsAt = audioCoverage.GetProperty("trackMatchStartsAt").GetDouble() + audioCoverage.GetProperty("trackCoverageLength").GetDouble(),

            QueryCoverageLength = audioCoverage.GetProperty("queryCoverageLength").GetDouble(),
            TrackCoverageLength = audioCoverage.GetProperty("trackCoverageLength").GetDouble(),

            QueryCoverage = audioCoverage.GetProperty("queryCoverage").GetDouble(),
            TrackCoverage = audioCoverage.GetProperty("trackCoverage").GetDouble(),
        };
    }
}
