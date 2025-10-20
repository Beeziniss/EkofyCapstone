using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.ArtistPackages;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.ArtistPackages
{
    public class ArtistPackageService : IArtistPackageService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ArtistPackageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IQueryable<ArtistPackage> GetArtistPackages()
        {
            return _unitOfWork.GetCollection<ArtistPackage>().AsQueryable().Where(ap => !ap.IsDelete);
        }

        public async Task CreateArtistPackageAsync(CreateArtistPackageRequest createRequest)
        {
            //?? tại sao chỗ này lại lấy version cao nhất rồi +1 nhỉ, ko hợp lý lắm
            //long currentVersion = await _unitOfWork.GetCollection<ArtistPackage>()
            //     .Find(x => x.PackageName == createRequest.PackageName && x.IsDelete != true )
            //     .SortByDescending(ap => ap.Version)
            //     .Project(ap => ap.Version)
            //     .FirstOrDefaultAsync();

            String newArtistPackageId = ObjectId.GenerateNewId().ToString();

            ArtistPackage newArtistPackage = new ArtistPackage
            {
                Id = newArtistPackageId,
                OriginPackageId = newArtistPackageId,
                ArtistId = createRequest.ArtistId,
                PackageName = createRequest.PackageName,
                Amount = createRequest.Amount,
                EstimateDeliveryDays = createRequest.EstimateDeliveryDays,
                Description = createRequest.Description,
                ServiceDetails = createRequest.ServiceDetails,
                Status = ArtistPackageStatus.Pending,
                Version = 1,
            };

            await _unitOfWork.GetCollection<ArtistPackage>().InsertOneAsync(newArtistPackage);
        }

        public async Task UpdateArtistPackageAsync(UpdateArtistPackageRequest updateRequest)
        {

            long currentVersion = await _unitOfWork.GetCollection<ArtistPackage>()
             .Find(x => x.Id == updateRequest.Id && !x.IsDelete)
             .SortByDescending(ap => ap.Version)
             .Project(ap => ap.Version)
             .FirstOrDefaultAsync();

            // artist package thực chất không thay đổi mà tạo mới với version mới, tránh conflict với những gói mà người dùng đã mua
            var artistPackage = new ArtistPackage
            {
                PackageName = updateRequest.PackageName,
                Amount = updateRequest.Amount,
                OriginPackageId = updateRequest.OriginPackageId,
                EstimateDeliveryDays = updateRequest.EstimateDeliveryDays,
                Description = updateRequest.Description,
                ServiceDetails = updateRequest.ServiceDetails,
                IsDelete = updateRequest.IsDelete,
                Version = ++currentVersion,
            };

            await _unitOfWork.GetCollection<ArtistPackage>().InsertOneAsync(artistPackage);
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
                                                   .Find(ap => ap.Id == updateStatusRequest.Id)
                                                   .Project<ArtistPackage>(Builders<ArtistPackage>.Projection.Include(ap => ap.Status))
                                                   .FirstOrDefaultAsync();

            if (artistPackage.Status != ArtistPackageStatus.Enabled || artistPackage.Status != ArtistPackageStatus.Disabled)
            {
                throw new ForbiddenCustomException("This action can not be execute by artist.");
            }

            UpdateDefinition<ArtistPackage> updateDefinition = Builders<ArtistPackage>.Update.Set(ap => ap.Status, updateStatusRequest.Status);

            var result = await _unitOfWork.GetCollection<ArtistPackage>().UpdateOneAsync(ap => ap.Id == updateStatusRequest.Id && !ap.IsDelete, updateDefinition);

            if (result.MatchedCount == 0)
            {
                throw new NotFoundCustomException("Artist package not found.");
            }
            if (result.ModifiedCount == 0)
            {
                throw new BadRequestCustomException("No changes were made to the artist package status.");
            }
        }

        public async Task ApproveArtistPackageAsync(UpdateStatusArtistPackageRequest updateStatusRequest)
        {
            //Only moderator approve => Pending -> Active or Pending -> Rejected
            if (updateStatusRequest.Status != ArtistPackageStatus.Enabled || updateStatusRequest.Status != ArtistPackageStatus.Rejected)
            {
                throw new ForbiddenCustomException("This action can not be excecute by moderator.");
            }

            UpdateDefinition<ArtistPackage> updateDefinition = Builders<ArtistPackage>.Update.Set(ap => ap.Status, updateStatusRequest.Status);

            var result = await _unitOfWork.GetCollection<ArtistPackage>().UpdateOneAsync(ap => ap.Id == updateStatusRequest.Id, updateDefinition);

            if (result.MatchedCount == 0)
            {
                throw new NotFoundCustomException("Artist package not found.");
            }
            if (result.ModifiedCount == 0)
            {
                throw new BadRequestCustomException("No changes were made to the artist package status.");
            }
        }
    }
}
