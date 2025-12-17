using EkofyApp.Application.Models.AudioFingerprints;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ThirdPartyServiceInterfaces.EmySound;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using MongoDB.Driver;
using Refit;
using System.Text.Json;

namespace EkofyApp.Infrastructure.ThirdPartyServices.EmySound;
public sealed class EmySoundService(IEmySoundApi emySoundApi, IUnitOfWork unitOfWork) : IEmySoundService
{
    private readonly IEmySoundApi _emySoundApi = emySoundApi;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<string> UploadTrackFingerprintAsync(Stream stream, string trackId, string trackName, string artistName, string artistId)
    {
        StreamPart streamPart = new(stream, $"{trackName}.mp3", "audio/mpeg");
        string response = await _emySoundApi.UploadTrackAsync(streamPart, MediaType.Audio.ToString(), trackId, trackName, artistName, artistId);
        return response;
    }

    public async Task<IEnumerable<QueryAudioFingerprintResponse>> CheckTrackFingerprintAsync(byte[] fileBytes, string fileName, string contentType)
    {
        double minConfidence = await _unitOfWork.GetCollection<FingerprintConfidencePolicy>().Find(_ => true).FirstOrDefaultAsync() is FingerprintConfidencePolicy policy
            ? policy.RejectThreshold
            : 0.8;
        double minCoverage = 0.6;

        using MemoryStream firstStream = new(fileBytes);
        StreamPart firstStreamPart = new(firstStream, fileName, contentType);

        HttpResponseMessage response = await _emySoundApi.QueryTrackAsync(firstStreamPart, MediaType.Audio.ToString(), minConfidence, minCoverage);

        string body = await response.Content.ReadAsStringAsync();

        if (JsonDocument.Parse(body).RootElement.GetArrayLength() == 0)
        {
            minConfidence = await _unitOfWork.GetCollection<FingerprintConfidencePolicy>().Find(_ => true).FirstOrDefaultAsync() is FingerprintConfidencePolicy secondPolicy
            ? secondPolicy.RejectThreshold - 0.1
            : 0.7;
            minCoverage = 0.4;

            using MemoryStream secondStream = new(fileBytes);
            StreamPart secondStreamPart = new(secondStream, fileName, contentType);

            response = await _emySoundApi.QueryTrackAsync(secondStreamPart, MediaType.Audio.ToString(), minConfidence, minCoverage);
            body = await response.Content.ReadAsStringAsync();
        }

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
                ArtistId = track.GetProperty("metaFields").GetProperty("artistId").GetString() ?? throw new NotFoundCustomException("Artist UserId is empty."),
                MediaType = track.GetProperty("mediaType").GetString() ?? throw new NotFoundCustomException("Media Type is empty."),
                QueryMatchStartsAt = audioCoverage.GetProperty("queryMatchStartsAt").GetDouble(),
                QueryMatchEndsAt = audioCoverage.GetProperty("queryMatchStartsAt").GetDouble() + audioCoverage.GetProperty("queryCoverageLength").GetDouble(),
                TrackMatchStartsAt = audioCoverage.GetProperty("trackMatchStartsAt").GetDouble(),
                TrackMatchEndsAt = audioCoverage.GetProperty("trackMatchStartsAt").GetDouble() + audioCoverage.GetProperty("trackCoverageLength").GetDouble(),
                QueryCoverageLength = audioCoverage.GetProperty("queryCoverageLength").GetDouble(),
                TrackCoverageLength = audioCoverage.GetProperty("trackCoverageLength").GetDouble(),
                QueryCoverage = audioCoverage.GetProperty("queryCoverage").GetDouble(),
                TrackCoverage = audioCoverage.GetProperty("trackCoverage").GetDouble(),

                MinConfidence = minConfidence,
                MinCoverage = minCoverage,
            });
        }

        return results;
    }
}
