using EkofyApp.Application.Models.Wavs;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HealthyNutritionApp.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Data;
using SoundFingerprinting.InMemory;
using SoundFingerprinting.Query;

namespace EkofyApp.Infrastructure.Services.Tracks;
public class AudioFingerprintService(IUnitOfWork unitOfWork) : IAudioFingerprintService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<AudioFingerprint> GenerateFingerprint(WavFileResponse wavFileResponse)
    {
        SoundFingerprintingAudioService audioService = new();

        AVHashes hashes = await FingerprintCommandBuilder.Instance
            .BuildFingerprintCommand()
            .From(wavFileResponse.OutputWavPath)
            .UsingServices(audioService)
            .Hash();

        if (hashes.Audio is null)
        {
            throw new InvalidOperationException("No audio fingerprints were generated. Please check the input file.");
        }

        AudioFingerprint audioFingerprint = new()
        {
            CompressedFingerprints = hashes.Audio.Select(h => DataEncryptionExtensions.CompressIntArray(h.HashBins)).ToList(),
            SequenceNumbers = hashes.Audio.Select(h => h.SequenceNumber).ToList(),
            StartsAt = hashes.Audio.Select(h => h.StartsAt).ToList(),
            OriginalPoints = hashes.Audio.Select(h => h.OriginalPoint).ToList(),
            Duration = hashes.Audio.DurationInSeconds,
            CreatedAt = TimeControl.GetUtcPlus7Time(),
            UpdatedAt = null
        };

        return audioFingerprint;
    }

    public async Task<double> CompareWithDatabase(WavFileResponse wavFileResponse)
    {
        SoundFingerprintingAudioService audioService = new();
        double bestConfidence = 0;

        try
        {
            await FingerprintCommandBuilder.Instance
            .BuildFingerprintCommand()
            .From(wavFileResponse.OutputWavPath)
            .UsingServices(audioService)
            .Hash();

            List<AudioFingerprint> audioFingerprints = await _unitOfWork.GetCollection<Track>().Find(_ => true)
                .Project(track => track.AudioFingerprint)
                .ToListAsync();


            InMemoryModelService tempModelService = new();

            foreach (AudioFingerprint audioFingerprint in audioFingerprints)
            {

                TrackInfo track = new("track_name_temp", "temp", "unknown");

                HashedFingerprint[] hashesFromDb = new HashedFingerprint[audioFingerprint.CompressedFingerprints.Count];
                for (int i = 0; i < audioFingerprint.CompressedFingerprints.Count; i++)
                {
                    hashesFromDb[i] = new HashedFingerprint(
                        DataEncryptionExtensions.DecompressToIntArray(audioFingerprint.CompressedFingerprints[i]),
                        audioFingerprint.SequenceNumbers[i],
                        audioFingerprint.StartsAt[i],
                        audioFingerprint.OriginalPoints[i]);
                }

                Hashes audioHashes = new(hashesFromDb, audioFingerprint.CompressedFingerprints.Count * 0.928, MediaType.Audio);

                AVHashes avHashes = new(audioHashes, null);

                tempModelService.Insert(track, avHashes);

                AVQueryResult queryResult = await QueryCommandBuilder.Instance
                    .BuildQueryCommand()
                    .From(wavFileResponse.OutputWavPath)
                    .UsingServices(tempModelService, audioService)
                    .Query();

                if (queryResult.BestMatch?.Audio.Confidence * 100 > bestConfidence)
                {
                    bestConfidence = queryResult.BestMatch.Audio.Confidence * 100;
                }
            }
        }
        catch
        {
            // Nếu có lỗi xảy ra trong quá trình so sánh, không cần làm gì cả
            if (File.Exists(wavFileResponse.OutputWavPath))
            {
                File.Delete(wavFileResponse.OutputWavPath);
            }
        }

        return bestConfidence;
    }
}
