using EkofyApp.Application.Models.Projections;
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
                    .Unwind<MonthlyStreamCountProjection, MonthlyStreamCountProjection>(x => x.RecordingProjection, new AggregateUnwindOptions<MonthlyStreamCountProjection> { PreserveNullAndEmptyArrays = true })
                    .Lookup<MonthlyStreamCountProjection, Work, MonthlyStreamCountProjection>(
                        _unitOfWork.GetCollection<Work>(),
                        x => x.TrackId,
                        x => x.TrackId,
                        x => x.WorkProjection)
                    .Unwind<MonthlyStreamCountProjection, MonthlyStreamCountProjection>(x => x.WorkProjection, new AggregateUnwindOptions<MonthlyStreamCountProjection> { PreserveNullAndEmptyArrays = true })
                    .Limit(limit)
                    .ToListAsync(ct);

            if (monthlyStreamCountProjections.Count == 0)
            {
                throw new NotFoundCustomException($"No MonthlyStreamCount records found for month={month}, year={year} to process.");
            }

            foreach (MonthlyStreamCountProjection monthlyStreamCountProjection in monthlyStreamCountProjections)
            {
                decimal totalRoyalty = monthlyStreamCountProjection.StreamCount * ratePerStream;

                List<RoyaltySplit> splits = [];

                // Nếu có RecordingId → áp dụng RecordingSplits
                decimal recordingPool = totalRoyalty * recordingRoyaltyPercentage / 100m;
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
                                IsTransferred = false, // Đã có đánh dấu chuyển tiền ở bước sau
                            });
                        }
                    }
                }

                // Nếu có WorkId → áp dụng WorkSplits
                decimal workPool = totalRoyalty * workRoyaltyPercentage / 100m;
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
                                IsTransferred = false, // Đã có đánh dấu chuyển tiền ở bước sau
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
                    Id = ObjectId.GenerateNewId().ToString(),
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
            //        PackageDescription = $"Royalty payout for {month}/{year}"
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
            //        PackageDescription = transfer.PackageDescription,
            //    };

            //    payoutTransactions.Add(payoutTransaction);
            //}
            #endregion

            // Nhóm splits theo userId để tối ưu số lượng payout
            var groupedSplits = royaltyReports
                .SelectMany(r => r.RoyaltySplits.Select(s => new { Report = r, Split = s }))
                .GroupBy(x => x.Split.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var userGroup in groupedSplits)
            {
                string userId = userGroup.Key;
                var userSplits = userGroup.Value;

                // Lấy artistStripeAccountId từ UserId
                string? artistStripeAccountId = await _unitOfWork.GetCollection<User>()
                    .Find(x => x.Id == userId)
                    .Project(x => x.StripeAccountId)
                    .FirstOrDefaultAsync(ct);

                if (string.IsNullOrEmpty(artistStripeAccountId))
                {
                    _logger.LogWarning($"Skipping payout for userId={userId} due to missing StripeAccountId");
                    // Đánh dấu các splits này là không thể transfer
                    foreach (var item in userSplits)
                    {
                        item.Split.IsTransferred = false;
                    }
                    continue;
                }

                // Tính tổng amount cho user này
                decimal totalVndAmount = userSplits.Sum(x => x.Split.Amount);
                decimal totalSgdAmount = HelperCurrencyConverter.ConvertVndToSgd(totalVndAmount);

                try
                {
                    long stripeTotalAmountLong = HelperCurrencyConverter.ConvertDecimalToStripeAmount(totalSgdAmount, CurrencyType.sgd.ToString());

                    // Thực hiện transfer trước, sau đó payout
                    TransferService transferService = new();
                    Transfer transferResponse = transferService.Create(new TransferCreateOptions
                    {
                        Amount = stripeTotalAmountLong,
                        Currency = CurrencyType.sgd.ToString(), // Sử dụng SGD cho sandbox
                        Destination = artistStripeAccountId,
                        TransferGroup = $"royalty-{month}-{year}",
                        Description = $"Royalty transfer for {month}/{year}"
                    });

                    // Đợi một chút để transfer được xử lý
                    await Task.Delay(3000, ct);

                    // Kiểm tra balance của connected account trước khi payout
                    Balance accountBalance = await _stripeService.GetConnectedAccountBalanceAsync(artistStripeAccountId);
                    long availableBalance = accountBalance.Available.FirstOrDefault()?.Amount ?? 0;

                    if (availableBalance < stripeTotalAmountLong)
                    {
                        _logger.LogError($"Insufficient balance for userId={userId}. Available: {availableBalance}, Required: {totalSgdAmount}");

                        continue;
                    }

                    // Thực hiện payout thực sự
                    Payout payoutResponse = await _stripeService.CreateInstantPayoutAsync(artistStripeAccountId, stripeTotalAmountLong, CurrencyType.sgd.ToString());

                    // Cập nhật royalty earnings cho Artist
                    UpdateResult updateArtistRoyaltyResult = await _unitOfWork.GetCollection<ArtistRevenue>()
                        .UpdateOneAsync(session,
                            x => x.UserId == userId,
                            Builders<ArtistRevenue>.Update
                                .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
                                .Inc(x => x.RoyaltyEarnings, totalVndAmount),
                            new UpdateOptions { IsUpsert = true },
                            cancellationToken: ct);
                    if (updateArtistRoyaltyResult.ModifiedCount == 0)
                    {
                        _logger.LogError($"Failed to update ArtistRevenue for userId={userId} with royalty earnings.");
                    }

                    // Cập nhật payout royalty amount cho Platform
                    UpdateResult updatePayoutRoyaltyResult = await _unitOfWork.GetCollection<PlatformRevenue>()
                        .UpdateOneAsync(session,
                            _ => true,
                            Builders<PlatformRevenue>.Update
                                .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
                                .Inc(x => x.RoyaltyPayoutAmount, totalVndAmount),
                            new UpdateOptions { IsUpsert = true },
                            cancellationToken: ct);
                    if (updatePayoutRoyaltyResult.ModifiedCount == 0)
                    {
                        _logger.LogError("Failed to update PlatformRevenue with payout royalty amount.");
                    }

                    // Lưu transaction cho từng report
                    foreach (var item in userSplits)
                    {
                        PayoutTransaction payoutTransaction = new()
                        {
                            UserId = userId,
                            RoyaltyReportId = item.Report.Id,
                            StripeTransferId = transferResponse.Id,
                            StripePayoutId = payoutResponse.Id,
                            Amount = item.Split.Amount,
                            Currency = CurrencyType.vnd.ToString(),
                            DestinationAccountId = artistStripeAccountId,
                            Level = item.Split.Level,
                            Description = payoutResponse.Description,
                            Status = Enum.Parse<PayoutTransactionStatus>(payoutResponse.Status), // pending, in_transit
                            Method = payoutResponse.Method, // standard hoặc instant
                        };

                        payoutTransactions.Add(payoutTransaction);

                        // Đánh dấu là đã được transfer/payout
                        item.Split.IsTransferred = true;
                    }
                }
                catch (Exception ex)
                {
                    // Đánh dấu các splits này là không thể transfer
                    foreach (var item in userSplits)
                    {
                        item.Split.IsTransferred = false;
                    }

                    // Có thể throw hoặc continue tùy business logic
                    throw new BadRequestCustomException($"Failed to process payout for user {userId}: {ex.Message}");
                }
            }

            // Lưu Payout Payout Transaction
            if (payoutTransactions.Count == 0)
            {
                //_logger.LogWarning($"No payout transactions were created for month={month}, year={year}. Skipping further processing.");
                throw new UnprocessableEntityCustomException($"No payout transactions were created for month={month}, year={year}. Skipping further processing.");
            }

            // Lưu Royalty Report
            if (royaltyReports.Count == 0)
            {
                //_logger.LogWarning($"No royalty reports were generated for month={month}, year={year}. Skipping further processing.");
                throw new UnprocessableEntityCustomException($"No royalty reports were generated for month={month}, year={year}. Skipping further processing.");
            }

            await _unitOfWork.GetCollection<PayoutTransaction>().InsertManyAsync(session, payoutTransactions, cancellationToken: ct);
            await _unitOfWork.GetCollection<RoyaltyReport>().InsertManyAsync(session, royaltyReports, cancellationToken: ct);

            // Cập nhật lại trạng thái đã xử lý
            UpdateDefinition<MonthlyStreamCount> updateDefinition = Builders<MonthlyStreamCount>.Update.Set(x => x.ProcessedAt, HelperMethod.GetUtcPlus7TimeOffset());
            UpdateResult updateResult = await _unitOfWork.GetCollection<MonthlyStreamCount>()
                .UpdateManyAsync(session,
                    x => processedMonthlyStreamCountIds.Contains(x.Id),
                    updateDefinition,
                    cancellationToken: ct);
            if (updateResult.ModifiedCount == 0)
            {
                throw new UnprocessableEntityCustomException($"Failed to update MonthlyStreamCount as processed.");
            }
        });
    }
}
