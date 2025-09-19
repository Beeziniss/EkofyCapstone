using EkofyApp.Application.Models.Projections;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.RoyaltyReports;
public sealed class RoyaltyReportService(IUnitOfWork unitOfWork, IRedisCacheService redisCacheService) : IRoyaltyReportService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    private async Task<Dictionary<string, string?>> GetRoyaltyPolicyValuesAsync(string key, params string[] fields)
    {
        var tasks = fields.Select(field =>
            _redisCacheService.HashGetAsync(key, field)
                .ContinueWith(t => new { Field = field, Value = t.Result }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        return results.ToDictionary(r => r.Field, r => r.Value);
    }

    public async Task GenerateMonthlyRoyaltyReportsAsync(int month, int year, CancellationToken ct = default)
    {
        //decimal ratePerStream = Convert.ToDecimal((await _redisCacheService.HashGetAsync("royalty_policy:active", "ratePerStream")));
        //decimal recordingRoyaltyPercentage = Convert.ToDecimal((await _redisCacheService.HashGetAsync("royalty_policy:active", "RecordingPercentage")));
        //decimal workRoyaltyPercentage = Convert.ToDecimal((await _redisCacheService.HashGetAsync("royalty_policy:active", "WorkPercentage")));

        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            List<RoyaltyReport> royaltyReports = [];
            List<string> processedMonthlyStreamCountIds = [];

            // Get all royalty policy values in a single Redis call
            string[] royaltyPolicyFields = ["rate_per_stream", "recording_percentage", "work_percentage"];
            Dictionary<string, string?> royaltyPolicyValues = await GetRoyaltyPolicyValuesAsync("royalty_policy:active", royaltyPolicyFields);

            decimal ratePerStream = Convert.ToDecimal(royaltyPolicyValues["rate_per_stream"]);
            decimal recordingRoyaltyPercentage = Convert.ToDecimal(royaltyPolicyValues["recording_percentage"]);
            decimal workRoyaltyPercentage = Convert.ToDecimal(royaltyPolicyValues["work_percentage"]);

            ProjectionDefinition<MonthlyStreamCountProjection> projectionDefinition = Builders<MonthlyStreamCountProjection>.Projection
                    .Exclude(x => x.CreatedAt)
                    .Exclude(x => x.Level);

            // Lấy toàn bộ thống kê theo tháng
            List<MonthlyStreamCountProjection> monthlyStreamCountProjections = await _unitOfWork.GetCollection<MonthlyStreamCount>()
                .Aggregate()
                .Match(x => x.Month == month && x.Year == year && x.ProcessedAt == null)
                .Lookup<MonthlyStreamCount, Recording, MonthlyStreamCountProjection>(
                    _unitOfWork.GetCollection<Recording>(),
                    x => x.RecordingId,
                    x => x.Id,
                    x => x.RecordingProjection)
                .Lookup<MonthlyStreamCountProjection, Work, MonthlyStreamCountProjection>(
                    _unitOfWork.GetCollection<Work>(),
                    x => x.WorkId,
                    x => x.Id,
                    x => x.WorkProjection)
                .Project<MonthlyStreamCountProjection>(projectionDefinition)
                .ToListAsync(ct);

            foreach (MonthlyStreamCountProjection monthlyStreamCountProjection in monthlyStreamCountProjections)
            {
                decimal totalRoyalty = monthlyStreamCountProjection.StreamCount * ratePerStream;

                List<RoyaltySplit> splits = [];

                // Nếu có RecordingId → áp dụng RecordingSplits
                decimal recordingPool = totalRoyalty * recordingRoyaltyPercentage;
                if (!string.IsNullOrEmpty(monthlyStreamCountProjection.RecordingId))
                {
                    if (monthlyStreamCountProjection.RecordingProjection != null)
                    {
                        // Kiểm tra tổng phần trăm có bằng 100% không
                        // Đã có validate ở cấp độ Recording rồi, nhưng để chắc chắn thì vẫn kiểm tra lại ở đây
                        decimal totalPercentage = monthlyStreamCountProjection.RecordingProjection.RecordingSplits.Sum(s => s.Percentage);
                        if (totalPercentage != 100m)
                        {
                            throw new ConflictCustomException($"Recording splits for {monthlyStreamCountProjection.RecordingId} must equal 100%, but got {totalPercentage}%");
                        }

                        foreach (RecordingSplitProjection split in monthlyStreamCountProjection.RecordingProjection.RecordingSplits)
                        {
                            decimal amount = recordingPool * split.Percentage / 100m;
                            splits.Add(new RoyaltySplit
                            {
                                UserId = split.UserId,
                                ArtistRole = split.ArtistRole,
                                Percentage = split.Percentage,
                                Amount = amount,
                                Level = AggregationLevel.Recording
                            });
                        }
                    }
                }

                // Nếu có WorkId → áp dụng WorkSplits
                decimal workPool = totalRoyalty * workRoyaltyPercentage;
                if (!string.IsNullOrEmpty(monthlyStreamCountProjection.WorkId))
                {
                    if (monthlyStreamCountProjection.WorkProjection != null)
                    {
                        // Kiểm tra tổng phần trăm có bằng 100% không
                        // Đã có validate ở cấp độ Recording rồi, nhưng để chắc chắn thì vẫn kiểm tra lại ở đây
                        decimal totalPercentage = monthlyStreamCountProjection.WorkProjection.WorkSplits.Sum(s => s.Percentage);
                        if (totalPercentage != 100m)
                        {
                            throw new ConflictCustomException($"Work splits for {monthlyStreamCountProjection.WorkId} must equal 100%, but got {totalPercentage}%");
                        }

                        foreach (WorkSplitProjection split in monthlyStreamCountProjection.WorkProjection.WorkSplits)
                        {
                            decimal amount = workPool * split.Percentage / 100m;
                            splits.Add(new RoyaltySplit
                            {
                                UserId = split.UserId,
                                ArtistRole = split.ArtistRole,
                                Percentage = split.Percentage,
                                Amount = amount,
                                Level = AggregationLevel.Work
                            });
                        }
                    }
                }

                // --- Validate tổng ---
                decimal distributed = splits.Sum(s => s.Amount);
                if (Math.Round(distributed, 2) != Math.Round(totalRoyalty, 2))
                {
                    throw new ConflictCustomException($"Distributed {distributed} != TotalRoyalty {totalRoyalty}");
                }

                RoyaltyReport report = new()
                {
                    TrackId = monthlyStreamCountProjection.TrackId,
                    Month = monthlyStreamCountProjection.Month,
                    Year = monthlyStreamCountProjection.Year,
                    StreamCount = monthlyStreamCountProjection.StreamCount,
                    TotalRoyaltyAmount = totalRoyalty,
                    RoyaltySplits = splits,
                };

                royaltyReports.Add(report);
                processedMonthlyStreamCountIds.Add(monthlyStreamCountProjection.Id);
            }
            // Tạo Royalty Report
            await _unitOfWork.GetCollection<RoyaltyReport>().InsertManyAsync(session, royaltyReports, cancellationToken: ct);

            // Cập nhật lại trạng thái đã xử lý
            UpdateDefinition<MonthlyStreamCount> updateDefinition = Builders<MonthlyStreamCount>.Update.Set(x => x.ProcessedAt, HelperMethod.GetUtcPlus7TimeOffset());
            UpdateResult updateResult = await _unitOfWork.GetCollection<MonthlyStreamCount>()
                .UpdateManyAsync(session,
                    x => processedMonthlyStreamCountIds.Contains(x.Id),
                    updateDefinition,
                    cancellationToken: ct);
            if (updateResult.ModifiedCount < processedMonthlyStreamCountIds.Count)
            {
                throw new Exception($"Failed to update MonthlyStreamCount as processed.");
            }
        });
    }
}
