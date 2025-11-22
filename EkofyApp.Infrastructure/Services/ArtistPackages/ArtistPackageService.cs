using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.ArtistPackages;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.ArtistPackages
{
    public class ArtistPackageService(IUnitOfWork unitOfWork, IRedisCacheService redisCacheService) : IArtistPackageService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IRedisCacheService _redisCacheService = redisCacheService;

        public IQueryable<ArtistPackage> GetArtistPackages()
        {
            return _unitOfWork.GetCollection<ArtistPackage>().AsQueryable().Where(ap => !ap.IsDelete);
        }

        public IQueryable<ArtistPackage> GetArtistPackagesInConversation(string artistId)
        {
            return _unitOfWork.GetCollection<ArtistPackage>().AsQueryable()
                .Where(ap => !ap.IsDelete && ap.Status == ArtistPackageStatus.Enabled && ap.ArtistId == artistId);
        }

        public async Task CreateArtistPackageAsync(CreateArtistPackageRequest createRequest)
        {
            string newArtistPackageId = ObjectId.GenerateNewId().ToString();

            ArtistPackage newArtistPackage = new ()
            {
                Id = newArtistPackageId,
                ArtistId = createRequest.ArtistId,
                PackageName = createRequest.PackageName,
                Amount = createRequest.Amount,
                EstimateDeliveryDays = createRequest.EstimateDeliveryDays,
                Description = createRequest.Description,
                ServiceDetails = createRequest.ServiceDetails,
                MaxRevision = createRequest.MaxRevision,
                Status = ArtistPackageStatus.Enabled,
            };

            // Save to Redis for moderation
            //var pendingPackage = new PendingArtistPackageResponse
            //{
            //    Id = newArtistPackage.Id,
            //    ArtistId = newArtistPackage.ArtistId,
            //    PackageName = newArtistPackage.PackageName,
            //    Amount = newArtistPackage.Amount,
            //    Currency = newArtistPackage.Currency,
            //    EstimateDeliveryDays = newArtistPackage.EstimateDeliveryDays,
            //    Description = newArtistPackage.Description,
            //    ServiceDetails = newArtistPackage.ServiceDetails,
            //    Status = newArtistPackage.Status,
            //    RequestedAt = newArtistPackage.CreatedAt
            //};

            //string redisKey = $"artistpackage:{newArtistPackageId}:pending";
            //TimeSpan expiry = TimeSpan.FromDays(3); // Cache for 7 days

            //await _redisCacheService.SetGenericAsync(redisKey, pendingPackage, expiry);

            // insert directly to database for now
            await _unitOfWork.GetCollection<ArtistPackage>().InsertOneAsync(newArtistPackage);
        }

        public async Task UpdateArtistPackageAsync(UpdateArtistPackageRequest updateRequest)
        {
            List<UpdateDefinition<ArtistPackage>> updates = [];

            if (!string.IsNullOrWhiteSpace(updateRequest.PackageName))
            {
                updates.Add(Builders<ArtistPackage>.Update.Set(x => x.PackageName, updateRequest.PackageName));
            }

            if (!string.IsNullOrWhiteSpace(updateRequest.Description))
            {
                updates.Add(Builders<ArtistPackage>.Update.Set(x => x.Description, updateRequest.Description));
            }

            if (updates.Count == 0)
            {
                throw new BadRequestCustomException("No valid fields to update.");
            }

            UpdateDefinition<ArtistPackage> updateDefinition = Builders<ArtistPackage>.Update.Combine(updates);

            UpdateResult result = await _unitOfWork.GetCollection<ArtistPackage>()
                .UpdateOneAsync(x => x.Id == updateRequest.Id && !x.IsDelete, updateDefinition);
            if (result.MatchedCount == 0)
            {
                throw new NotFoundCustomException("Artist package not found.");
            }
            if (result.ModifiedCount == 0)
            {
                throw new UnprocessableEntityCustomException("No changes were made to the artist package.");
            }
        }

        public async Task DeleteArtistPackageAsync(string id)
        {
            var updateDefinition = Builders<ArtistPackage>.Update.Set(ap => ap.IsDelete, true);
            var result = await _unitOfWork.GetCollection<ArtistPackage>().UpdateOneAsync(ap => ap.Id == id && !ap.IsDelete, updateDefinition);
            if (result.MatchedCount == 0)
            {
                throw new NotFoundCustomException("Artist package not found.");
            }
            if (result.ModifiedCount == 0)
            {
                throw new BadRequestCustomException("No changes were made to the artist package.");
            }
        }

        public async Task ChangeArtistPackageStatusAsync(UpdateStatusArtistPackageRequest updateStatusRequest)
        {
            var artistPackage = await _unitOfWork.GetCollection<ArtistPackage>()
                                                   .Find(ap => ap.Id == updateStatusRequest.Id && !ap.IsDelete)
                                                   .Project<ArtistPackage>(Builders<ArtistPackage>.Projection.Include(ap => ap.Status))
                                                   .FirstOrDefaultAsync();

            if (artistPackage == null)
            {
                throw new NotFoundCustomException("Artist package not found.");
            }

            // Only artists can toggle between Enabled/Disabled status
            if (artistPackage.Status != ArtistPackageStatus.Enabled && artistPackage.Status != ArtistPackageStatus.Disabled)
            {
                throw new ForbiddenCustomException("This action can not be executed by artist. Package must be approved first.");
            }

            // Artists can only toggle between Enabled and Disabled
            if (updateStatusRequest.Status != ArtistPackageStatus.Enabled && updateStatusRequest.Status != ArtistPackageStatus.Disabled)
            {
                throw new ForbiddenCustomException("Artists can only enable or disable their packages.");
            }

            UpdateDefinition<ArtistPackage> updateDefinition = Builders<ArtistPackage>.Update.Set(ap => ap.Status, updateStatusRequest.Status);

            var result = await _unitOfWork.GetCollection<ArtistPackage>().UpdateOneAsync(ap => ap.Id == updateStatusRequest.Id && !ap.IsDelete, updateDefinition);

            if (result.ModifiedCount == 0)
            {
                throw new BadRequestCustomException("No changes were made to the artist package status.");
            }
        }

        //public async Task ApproveArtistPackageAsync(string id)
        //{
        //    // Get package info from Redis first
        //    string redisKey = $"artistpackage:{id}:pending";
        //    ICacheResult<PendingArtistPackageResponse> cacheResult = await _redisCacheService.TryGetGenericAsync<PendingArtistPackageResponse>(redisKey);

        //    if (!cacheResult.Success || cacheResult.Value == null)
        //    {
        //        throw new NotFoundCustomException("Pending artist package not found in cache or has expired.");
        //    }

        //    var pendingPackage = cacheResult.Value;

        //    if (pendingPackage.Status != ArtistPackageStatus.Pending)
        //    {
        //        throw new ForbiddenCustomException("Only pending packages can be approved.");
        //    }

        //    // Create approved package from pending data and insert into database
        //    var approvedPackage = new ArtistPackage
        //    {
        //        Id = pendingPackage.Id,
        //        ArtistId = pendingPackage.ArtistId,
        //        PackageName = pendingPackage.PackageName,
        //        Amount = pendingPackage.Amount,
        //        Currency = pendingPackage.Currency,
        //        EstimateDeliveryDays = pendingPackage.EstimateDeliveryDays,
        //        Description = pendingPackage.Description,
        //        ServiceDetails = pendingPackage.ServiceDetails,
        //        Status = ArtistPackageStatus.Enabled,
        //        IsDelete = false
        //    };

        //    // Insert new approved package
        //    await _unitOfWork.GetCollection<ArtistPackage>().InsertOneAsync(approvedPackage);

        //    // Remove from Redis cache after approval
        //    await _redisCacheService.RemoveAsync(redisKey);
        //}

        //public async Task RejectArtistPackageAsync(string id)
        //{
        //    // Get package info from Redis first
        //    string redisKey = $"artistpackage:{id}:pending";
        //    ICacheResult<PendingArtistPackageResponse> cacheResult = await _redisCacheService.TryGetGenericAsync<PendingArtistPackageResponse>(redisKey);

        //    if (!cacheResult.Success || cacheResult.Value == null)
        //    {
        //        throw new NotFoundCustomException("Pending artist package not found in cache or has expired.");
        //    }

        //    var pendingPackage = cacheResult.Value;

        //    if (pendingPackage.Status != ArtistPackageStatus.Pending)
        //    {
        //        throw new ForbiddenCustomException("Only pending packages can be rejected.");
        //    }

        //    // Remove from Redis cache after rejection
        //    await _redisCacheService.RemoveAsync(redisKey);
        //}


        //public async Task<PaginatedData<PendingArtistPackageResponse>> GetPendingArtistPackagesAsync(int pageNumber = 1, int pageSize = 20)
        //{
        //    ICacheResult<PaginatedData<PendingArtistPackageResponse>> result = await _redisCacheService.GetPendingArtistPackagesAsync(pageNumber, pageSize);

        //    PaginatedData<PendingArtistPackageResponse> paginatedData;

        //    if (!result.Success || result.Value == null)
        //    {
        //        return paginatedData = new()
        //        {
        //            Items = Enumerable.Empty<PendingArtistPackageResponse>(),
        //            TotalCount = 0
        //        };
        //    }

        //    paginatedData = new()
        //    {
        //        Items = result.Value.Items.Select(pending => new PendingArtistPackageResponse
        //        {
        //            Id = pending.Id,
        //            ArtistId = pending.ArtistId,
        //            PackageName = pending.PackageName,
        //            Amount = pending.Amount,
        //            Currency = pending.Currency,
        //            EstimateDeliveryDays = pending.EstimateDeliveryDays,
        //            Description = pending.Description,
        //            ServiceDetails = pending.ServiceDetails,
        //            Status = pending.Status,
        //            RequestedAt = pending.RequestedAt,
        //        }),
        //        TotalCount = result.Value.TotalCount
        //    };

        //    return paginatedData;
        //}
    }
}
