using EkofyApp.Application.Models.Projections;
using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
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
                //decimal stripeAmount = HelperCurrencyConverter.FormatDecimalLiteral(totalVndAmount); // Stripe tính bằng cent

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
                    //decimal availableBalanceDecimal = HelperCurrencyConverter.ConvertStripeAmountToDecimal(availableBalance, CurrencyType.sgd.ToString());
                    if (availableBalance < stripeTotalAmountLong)
                    {
                        _logger.LogError($"Insufficient balance for userId={userId}. Available: {availableBalance}, Required: {totalSgdAmount}");

                        continue;
                    }

                    // Thực hiện payout thực sự
                    Payout payoutResponse = await _stripeService.CreateInstantPayoutAsync(artistStripeAccountId, stripeTotalAmountLong,  CurrencyType.sgd.ToString());

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
                            Status = Enum.Parse<PayoutTransactionStatus>(payoutResponse.Status), // pending, paid, failed, canceled
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
            if (updateResult.ModifiedCount == 0)
            {
                throw new UnprocessableEntityCustomException($"Failed to update MonthlyStreamCount as processed.");
            }
        });
    }

    #region Chưa đụng đến
    /// <summary>
    /// Manual payout cho một artist cụ thể
    /// </summary>
    public async Task<bool> ProcessPayoutForArtistAsync(string artistId, decimal amount, bool isInstant = false, CancellationToken ct = default)
    {
        try
        {
            // Lấy thông tin artist
            string userId = await _unitOfWork.GetCollection<Artist>()
                .Find(x => x.UserId == artistId)
                .Project(x => x.UserId)
                .FirstOrDefaultAsync(ct) ?? throw new NotFoundCustomException($"Artist {artistId} not found");

            // Lấy thông tin user
            User user = await _unitOfWork.GetCollection<User>()
                .Find(x => x.Id == userId && x.Role == UserRole.Artist)
                .Project<User>(Builders<User>.Projection
                    .Include(x => x.Id)
                    .Include(x => x.FullName)
                    .Include(x => x.StripeAccountId))
                .FirstOrDefaultAsync(ct) ?? throw new NotFoundCustomException($"Artist for user {userId} not found");

            if (string.IsNullOrEmpty(user.StripeAccountId))
            {
                throw new BadRequestCustomException($"Artist {user.FullName} does not have Stripe account connected");
            }

            long stripeAmount = Convert.ToInt64(amount * 100); // Convert to cents

            // Kiểm tra balance
            Balance accountBalance = await _stripeService.GetConnectedAccountBalanceAsync(user.StripeAccountId);
            long availableBalance = accountBalance.Available.FirstOrDefault()?.Amount ?? 0;

            if (availableBalance < stripeAmount)
            {
                throw new BadRequestCustomException($"Insufficient balance. Available: ${availableBalance / 100.0:F2}, Required: ${amount:F2}");
            }

            // Thực hiện payout
            Payout payoutResponse = isInstant 
                ? await _stripeService.CreateInstantPayoutAsync(user.StripeAccountId, stripeAmount)
                : await _stripeService.CreatePayoutAsync(user.StripeAccountId, stripeAmount);

            // Lưu transaction
            PayoutTransaction payoutTransaction = new()
            {
                UserId = artistId,
                RoyaltyReportId = ObjectId.GenerateNewId().ToString(), // Manual payout không liên kết với specific report
                StripeTransferId = payoutResponse.Id,
                Amount = amount,
                Currency = payoutResponse.Currency,
                DestinationAccountId = user.StripeAccountId,
                Description = $"Manual {(isInstant ? "instant" : "standard")} payout for {user.FullName}",
                Status = Enum.Parse<PayoutTransactionStatus>(payoutResponse.Status),
                Method = payoutResponse.Method,
            };

            await _unitOfWork.GetCollection<PayoutTransaction>().InsertOneAsync(payoutTransaction, cancellationToken: ct);

            _logger.LogInformation($"Successfully processed manual payout for user {artistId}, amount=${amount:F2}, payoutId={payoutResponse.Id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process manual payout for user {artistId}, amount=${amount:F2}");
            throw;
        }
    }

    /// <summary>
    /// Batch payout cho tất cả artists có pending royalty trong tháng
    /// </summary>
    public async Task<bool> ProcessPayoutsForAllArtistsAsync(int month, int year, bool isInstant = false, CancellationToken ct = default)
    {
        try
        {
            // Lấy tất cả royalty reports chưa được payout trong tháng
            var pendingReports = await _unitOfWork.GetCollection<RoyaltyReport>()
                .Find(r => r.Month == month && r.Year == year && 
                          r.RoyaltySplits.Any(s => s.IsTransferred == false))
                .ToListAsync(ct);

            if (!pendingReports.Any())
            {
                _logger.LogInformation($"No pending royalty reports found for {month}/{year}");
                return true;
            }

            // Nhóm theo artistId và tính tổng amount
            var artistPayouts = pendingReports
                .SelectMany(r => r.RoyaltySplits.Where(s => !s.IsTransferred)
                    .Select(s => new { ArtistId = s.UserId, Amount = s.Amount, ReportId = r.Id, Split = s }))
                .GroupBy(x => x.ArtistId)
                .ToDictionary(g => g.Key, g => new { 
                    TotalAmount = g.Sum(x => x.Amount),
                    Items = g.ToList()
                });

            int successCount = 0;
            int failCount = 0;

            foreach (var artistPayout in artistPayouts)
            {
                try
                {
                    string artistId = artistPayout.Key;
                    decimal totalAmount = artistPayout.Value.TotalAmount;

                    // Process payout for this user
                    await ProcessPayoutForArtistAsync(artistId, totalAmount, isInstant, ct);

                    // Mark all splits as transferred
                    var reportIds = artistPayout.Value.Items.Select(x => x.ReportId).Distinct();
                    foreach (string reportId in reportIds)
                    {
                        UpdateDefinition<RoyaltyReport> updateDefinition = Builders<RoyaltyReport>.Update
                            .Set("RoyaltySplits.$[elem].IsTransferred", true);

                        ArrayFilterDefinition<RoyaltyReport> arrayFilter = new BsonDocumentArrayFilterDefinition<RoyaltyReport>(
    new BsonDocument("elem.UserId", artistId));

                        await _unitOfWork.GetCollection<RoyaltyReport>()
                            .UpdateOneAsync(
                                x => x.Id == reportId,
                                updateDefinition,
                                new UpdateOptions { ArrayFilters = new[] { arrayFilter } },
                                ct);
                    }

                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process batch payout for user {artistPayout.Key}");
                    failCount++;
                }
            }

            _logger.LogInformation($"Batch payout completed. Success: {successCount}, Failed: {failCount}");
            return failCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process batch payouts for {month}/{year}");
            throw;
        }
    }
    #endregion
}
