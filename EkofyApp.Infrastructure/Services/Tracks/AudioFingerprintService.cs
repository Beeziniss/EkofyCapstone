using EkofyApp.Application.Models.AudioFingerprints;
using EkofyApp.Application.Models.Wavs;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using MongoDB.Driver;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Data;
using SoundFingerprinting.InMemory;
using SoundFingerprinting.Query;

namespace EkofyApp.Infrastructure.Services.Tracks;
public sealed class AudioFingerprintService(IUnitOfWork unitOfWork) : IAudioFingerprintService
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
            CreatedAt = HelperMethod.GetUtcPlus7TimeOffset(),
            UpdatedAt = null
        };

        return audioFingerprint;
    }

    #region Sequantial Query
    public async Task<AudioFingerprintResult> GetMatchConfidenceScore(WavFileResponse wavFileResponse)
    {
        SoundFingerprintingAudioService audioService = new();
        double bestConfidence = 0;
        TrackInfo bestMatchTrack = null!;

        try
        {
            #region Analyze and Hash Audio Original
            await FingerprintCommandBuilder.Instance
                .BuildFingerprintCommand()
                .From(wavFileResponse.OutputWavPath)
                .UsingServices(audioService)
                .Hash();

            //IEnumerable<AudioFingerprint> audioFingerprints = await _unitOfWork.GetCollection<Track>().Find(_ => true)
            //    .Project(track => track.AudioFingerprint)
            //    .ToListAsync();

            ProjectionDefinition<Track> projection = Builders<Track>.Projection
                .Include(x => x.Id)
                .Include(x => x.Name)
                .Include(x => x.CreatedBy)
                .Include(x => x.AudioFingerprint);

            IEnumerable<Track> trackAudioFingerprints = await _unitOfWork.GetCollection<Track>().Find(_ => true)
                .Project<Track>(projection)
                .ToListAsync();

            InMemoryModelService tempModelService = new();

            foreach (Track trackAudioFingerprint in trackAudioFingerprints)
            {
                TrackInfo track = new(trackAudioFingerprint.Id, trackAudioFingerprint.Name, trackAudioFingerprint.CreatedBy);

                HashedFingerprint[] hashesFromDb = new HashedFingerprint[trackAudioFingerprint.AudioFingerprint.CompressedFingerprints.Count];
                for (int i = 0; i < trackAudioFingerprint.AudioFingerprint.CompressedFingerprints.Count; i++)
                {
                    hashesFromDb[i] = new HashedFingerprint(
                        DataEncryptionExtensions.DecompressToIntArray(trackAudioFingerprint.AudioFingerprint.CompressedFingerprints[i]),
                        trackAudioFingerprint.AudioFingerprint.SequenceNumbers[i],
                        trackAudioFingerprint.AudioFingerprint.StartsAt[i],
                        trackAudioFingerprint.AudioFingerprint.OriginalPoints[i]);
                }

                Hashes audioHashes = new(hashesFromDb, trackAudioFingerprint.AudioFingerprint.CompressedFingerprints.Count * 0.928, MediaType.Audio);

                AVHashes avHashes = new(audioHashes, null);

                tempModelService.Insert(track, avHashes);

                AVQueryResult queryResult = await QueryCommandBuilder.Instance
                    .BuildQueryCommand()
                    .From(wavFileResponse.OutputWavPath)
                    .UsingServices(tempModelService, audioService)
                    .Query();

                if (queryResult.BestMatch?.Audio?.Confidence * 100 > bestConfidence)
                {
                    bestConfidence = queryResult.BestMatch!.Audio!.Confidence * 100;
                    bestMatchTrack = track;
                }
            }
            #endregion
        }
        catch
        {
            // Nếu có lỗi xảy ra trong quá trình so sánh, không cần làm gì cả
            if (File.Exists(wavFileResponse.OutputWavPath))
            {
                File.Delete(wavFileResponse.OutputWavPath);
            }
        }
        finally
        {
            // Gọi GC để giải phóng bộ nhớ và đảm bảo không có rò rỉ bộ nhớ
            // Việc gọi GC không phải lúc nào cũng cần thiết, nhưng trong trường hợp này
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        return new()
        {
            BestConfidence = bestConfidence,
            TrackId = bestMatchTrack.Id,
            TrackName = bestMatchTrack.Title,
            ArtistId = bestMatchTrack.Artist
        };
    }
    #endregion

    #region Parallel Query
    //public async Task<double> GetMatchConfidenceScore(WavFileResponse wavFileResponse)
    //{
    //    double bestConfidence = 0;
    //    object lockObj = new(); // Đảm bảo thread-safe khi gán bestConfidence

    //    try
    //    {
    //        // Hash file WAV mới từ người dùng
    //        SoundFingerprintingAudioService audioService = new();
    //        AVHashes userAudioHashes = await FingerprintCommandBuilder.Instance
    //            .BuildFingerprintCommand()
    //            .From(wavFileResponse.OutputWavPath)
    //            .UsingServices(audioService)
    //            .Hash();

    //        // Lấy toàn bộ fingerprint từ DB
    //        IEnumerable<AudioFingerprint> audioFingerprints = await _unitOfWork
    //            .GetCollection<Track>()
    //            .Find(_ => true)
    //            .Project(track => track.AudioFingerprint)
    //            .ToListAsync();

    //        // So sánh song song
    //        await Parallel.ForEachAsync(audioFingerprints, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (fingerprint, _) =>
    //        {
    //            try
    //            {
    //                InMemoryModelService tempModelService = new(); // Dùng riêng mỗi task
    //                TrackInfo track = new("track_name_temp", "temp", "unknown");

    //                // Chuyển đổi fingerprint DB → HashedFingerprint[]
    //                HashedFingerprint[] hashesFromDb = new HashedFingerprint[fingerprint.CompressedFingerprints.Count];
    //                for (int i = 0; i < fingerprint.CompressedFingerprints.Count; i++)
    //                {
    //                    hashesFromDb[i] = new HashedFingerprint(
    //                        DataEncryptionExtensions.DecompressToIntArray(fingerprint.CompressedFingerprints[i]),
    //                        fingerprint.SequenceNumbers[i],
    //                        fingerprint.StartsAt[i],
    //                        fingerprint.OriginalPoints[i]);
    //                }

    //                Hashes audioHashes = new(hashesFromDb, hashesFromDb.Length * 0.928, MediaType.Audio);
    //                AVHashes avHashes = new(audioHashes, null);

    //                // Insert vào temp DB
    //                tempModelService.Insert(track, avHashes);

    //                // Query so sánh với hash của file WAV người dùng
    //                AVQueryResult queryResult = await QueryCommandBuilder.Instance
    //                    .BuildQueryCommand()
    //                    .From(wavFileResponse.OutputWavPath)
    //                    .UsingServices(tempModelService, audioService)
    //                    .Query();

    //                double confidence = (queryResult.BestMatch?.Audio.BestConfidence ?? 0) * 100;

    //                // Cập nhật kết quả tốt nhất một cách thread-safe
    //                lock (lockObj)
    //                {
    //                    if (confidence > bestConfidence)
    //                    {
    //                        bestConfidence = confidence;
    //                    }
    //                }
    //            }
    //            catch
    //            {
    //                // Ignore lỗi riêng lẻ trong mỗi task
    //                // Không cần làm gì cả, chỉ cần đảm bảo không làm gián đoạn toàn bộ quá trình
    //            }
    //        });
    //    }
    //    catch
    //    {
    //        // Lỗi tổng thể (như lỗi khi hash ban đầu) thì xoá file
    //    }
    //    finally
    //    {
    //        if (File.Exists(wavFileResponse.OutputWavPath))
    //        {
    //            File.Delete(wavFileResponse.OutputWavPath);
    //        }
    //    }

    //    return bestConfidence;
    //}
    #endregion
}
