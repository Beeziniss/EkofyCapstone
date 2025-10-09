using EkofyApp.Application.Models.Projections;
using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Stripe;

namespace EkofyApp.Infrastructure.Services.RoyaltyReports;
public sealed class RoyaltyReportService(IUnitOfWork unitOfWork, IRedisCacheService redisCacheService, IStripeService stripeService, ILogger<RoyaltyReportService> logger) : IRoyaltyReportService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly IStripeService _stripeService = stripeService;
    private readonly ILogger<RoyaltyReportService> _logger = logger;

    public IQueryable<RoyaltyReport> GetRoyaltyReports()
    {
        return _unitOfWork.GetCollection<RoyaltyReport>().AsQueryable();
    }

    private async Task<Dictionary<string, string?>> GetRoyaltyPolicyValuesAsync(string key, params string[] fields)
    {
        var tasks = fields.Select(field =>
            _redisCacheService.HashGetAsync(key, field)
                .ContinueWith(t => new { Field = field, Value = t.Result }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        return results.ToDictionary(r => r.Field, r => r.Value);
    }

    public async Task<long> GetTotalCountOfRoyaltyReportsAsync(int month, int year, CancellationToken ct = default)
    {
        FilterDefinition<MonthlyStreamCount> filter = Builders<MonthlyStreamCount>.Filter.Eq(r => r.Month, month) & Builders<MonthlyStreamCount>.Filter.Eq(r => r.Year, year) & Builders<MonthlyStreamCount>.Filter.Eq(r => r.ProcessedAt, null);
        return await _unitOfWork.GetCollection<MonthlyStreamCount>().CountDocumentsAsync(filter, cancellationToken: ct);
    }

    public async Task GenerateMonthlyRoyaltyReportsAsync(int month, int year, int limit = 100, CancellationToken ct = default)
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
                    .Exclude(x => x.CreatedAt);

            // Lấy toàn bộ thống kê theo tháng
            List<MonthlyStreamCountProjection> monthlyStreamCountProjections = await _unitOfWork.GetCollection<MonthlyStreamCount>()
                    .Aggregate()
                    .Match(x => x.Month == month && x.Year == year && x.ProcessedAt == null)
                    .Lookup<MonthlyStreamCount, Recording, MonthlyStreamCountProjection>(
                        _unitOfWork.GetCollection<Recording>(),
                        x => x.TrackId,
                        x => x.TrackId,
                        x => x.RecordingProjection)
                    .Lookup<MonthlyStreamCountProjection, Work, MonthlyStreamCountProjection>(
                        _unitOfWork.GetCollection<Work>(),
                        x => x.TrackId,
                        x => x.TrackId,
                        x => x.WorkProjection)
                    .Limit(limit)
                    .ToListAsync(ct);

            foreach (MonthlyStreamCountProjection monthlyStreamCountProjection in monthlyStreamCountProjections)
            {
                decimal totalRoyalty = monthlyStreamCountProjection.StreamCount * ratePerStream;

                List<RoyaltySplit> splits = [];

                // Nếu có RecordingId → áp dụng RecordingSplits
                decimal recordingPool = totalRoyalty * recordingRoyaltyPercentage;
                if (!string.IsNullOrEmpty(monthlyStreamCountProjection.RecordingProjection?.Id))
                {
                    if (monthlyStreamCountProjection.RecordingProjection != null)
                    {
                        // Kiểm tra tổng phần trăm có bằng 100% không
                        // Đã có validate ở cấp độ Recording rồi, nhưng để chắc chắn thì vẫn kiểm tra lại ở đây
                        decimal totalPercentage = monthlyStreamCountProjection.RecordingProjection.RecordingSplits.Sum(s => s.Percentage);
                        if (totalPercentage != 100m)
                        {
                            throw new ConflictCustomException($"Recording splits for {monthlyStreamCountProjection.RecordingProjection.Id} must equal 100%, but got {totalPercentage}%");
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
                                Level = AggregationLevel.Recording,
                                IsTransferred = true,
                            });
                        }
                    }
                }

                // Nếu có WorkId → áp dụng WorkSplits
                decimal workPool = totalRoyalty * workRoyaltyPercentage;
                if (!string.IsNullOrEmpty(monthlyStreamCountProjection.WorkProjection?.Id))
                {
                    if (monthlyStreamCountProjection.WorkProjection != null)
                    {
                        // Kiểm tra tổng phần trăm có bằng 100% không
                        // Đã có validate ở cấp độ Recording rồi, nhưng để chắc chắn thì vẫn kiểm tra lại ở đây
                        decimal totalPercentage = monthlyStreamCountProjection.WorkProjection.WorkSplits.Sum(s => s.Percentage);
                        if (totalPercentage != 100m)
                        {
                            throw new ConflictCustomException($"Work splits for {monthlyStreamCountProjection.WorkProjection.Id} must equal 100%, but got {totalPercentage}%");
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
                                Level = AggregationLevel.Work,
                                IsTransferred = true,
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

            // Transfer tiền royalty ở đây
            List<PayoutTransaction> payoutTransactions = [];

            #region Dùng dictionary để tối ưu nhưng chưa dùng được do dictionary không cho duplicate key và không trace được reportId
            //List<RoyaltySplit> royaltySplits = royaltyReports.SelectMany(r => r.RoyaltySplits).ToList();
            //Dictionary<string, decimal> userIdAmount = royaltySplits
            //    .GroupBy(s => s.UserId)
            //    .ToDictionary(g => g.Key, g => g.Sum(s => s.Amount));

            //var users = await _unitOfWork.GetCollection<User>()
            //    .Find(x => userIdAmount.ContainsKey(x.UserId))
            //    .Project(x => new { x.UserId, x.StripeAccountId })
            //    .ToListAsync(ct);

            //Dictionary<string, string?> userIdToStripeAccount = users
            //    .ToDictionary(k => k.UserId, v => v.StripeAccountId);

            //TransferService transferService = new();
            //string groupId = $"royalty-{month}-{year}-{ObjectId.GenerateNewId()}";

            //// Chuyển theo group
            //foreach (KeyValuePair<string, decimal> item in userIdAmount)
            //{
            //    if (string.IsNullOrEmpty(item.Key) || item.Value <= 0)
            //    {
            //        //throw new ConflictCustomException($"Invalid userId or amount for transfer: userId={item.Key}, amount={item.Value}");
            //        _logger.LogWarning($"Skipping transfer for userId={item.Key} due to invalid userId or amount={item.Value}");
            //        royaltySplits.FindAll(s => s.UserId == item.Key).ForEach(s => s.IsTransferred = false);
            //        continue;
            //    }

            //    if (!userIdToStripeAccount.TryGetValue(item.Key, out string? stripeAccountId) || string.IsNullOrEmpty(stripeAccountId))
            //    {
            //        //throw new ConflictCustomException($"Missing StripeAccountId for userId={item.Key}");
            //        _logger.LogWarning($"Skipping transfer for userId={item.Key} due to missing StripeAccountId");
            //        royaltySplits.FindAll(s => s.UserId == item.Key).ForEach(s => s.IsTransferred = false);
            //        continue;
            //    }

            //    Transfer transfer = transferService.Create(new TransferCreateOptions
            //    {
            //        Amount = Convert.ToInt64(item.Value), // Stripe amount cần long
            //        Currency = CurrencyType.vnd.ToString(),
            //        Destination = stripeAccountId,
            //        TransferGroup = groupId,
            //        Description = $"Royalty payout for {month}/{year}"
            //    });

            //    //Lưu transaction vào DB để trace
            //    PayoutTransaction payoutTransaction = new()
            //    {
            //        UserId = stripeAccountId,
            //        //RoyaltyReportId = ,
            //        StripeTransferId = transfer.UserId,
            //        Amount = transfer.Amount,
            //        Currency = transfer.Currency,
            //        DestinationAccountId = transfer.DestinationId,
            //        Description = transfer.Description,
            //    };

            //    payoutTransactions.Add(payoutTransaction);
            //}
            #endregion

            TransferService transferService = new();
            foreach (RoyaltyReport report in royaltyReports)
            {
                foreach (RoyaltySplit split in report.RoyaltySplits)
                {
                    // Lấy artistAccountId từ UserId
                    string? artistAccountId = await _unitOfWork.GetCollection<User>()
                        .Find(x => x.Id == split.UserId)
                        .Project(x => x.StripeAccountId) // giả sử bạn lưu ở đây
                        .FirstOrDefaultAsync(ct) ?? throw new NotFoundCustomException($"User {split.UserId} does not have stripe account."); ;

                    Transfer transferResponse = transferService.Create(new TransferCreateOptions
                    {
                        Amount = Convert.ToInt64(split.Amount), // Stripe amount cần long
                        Currency = CurrencyType.vnd.ToString(),
                        Destination = artistAccountId,
                        TransferGroup = $"royalty-{report.TrackId}-{month}-{year}-{HelperMethod.GetUtcPlus7TimeOffset().ToUnixTimeMilliseconds()}",
                        Description = $"Royalty payout for track {report.TrackId}: {month}/{year}"
                    });

                    // Lưu transaction vào DB để trace
                    PayoutTransaction payoutTransaction = new()
                    {
                        UserId = split.UserId,
                        RoyaltyReportId = report.Id,
                        StripeTransferId = transferResponse.Id,
                        Amount = Convert.ToDecimal(transferResponse.Amount),
                        Currency = transferResponse.Currency,
                        DestinationAccountId = transferResponse.DestinationId,
                        Description = transferResponse.Description,
                    };

                    //await _unitOfWork.GetCollection<PayoutTransaction>().InsertOneAsync(session, payoutTransaction, cancellationToken: ct);
                    payoutTransactions.Add(payoutTransaction);
                }
            }

            // Lưu Payout Payout Transaction
            if (payoutTransactions.Count == 0)
            {
                //_logger.LogWarning($"No payout transactions were created for month={month}, year={year}. Skipping further processing.");
                throw new UnprocessableEntityCustomException($"No payout transactions were created for month={month}, year={year}. Skipping further processing.");
            }

            await _unitOfWork.GetCollection<PayoutTransaction>().InsertManyAsync(session, payoutTransactions, cancellationToken: ct);

            // Lưu Royalty Report
            if (royaltyReports.Count == 0)
            {
                //_logger.LogWarning($"No royalty reports were generated for month={month}, year={year}. Skipping further processing.");
                throw new UnprocessableEntityCustomException($"No royalty reports were generated for month={month}, year={year}. Skipping further processing.");
            }

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
                throw new UnprocessableEntityCustomException($"Failed to update MonthlyStreamCount as processed.");
            }
        });
    }
}
