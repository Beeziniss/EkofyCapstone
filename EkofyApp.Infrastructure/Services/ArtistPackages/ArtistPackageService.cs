using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.ArtistPackages;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
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
            return _unitOfWork.GetCollection<ArtistPackage>().AsQueryable();
        }

        public async Task CreateArtistPackageAsync(CreateArtistPackageRequest createRequest)
        {
            await _unitOfWork.GetCollection<ArtistPackage>().InsertOneAsync(new ArtistPackage
            {
                Id = ObjectId.GenerateNewId().ToString(),
                PackageName = createRequest.PackageName,
                Price = createRequest.Price,
                EstimateDeliveryDays = createRequest.EstimateDeliveryDays,
                Description = createRequest.Description,
                ServiceDetails = createRequest.ServiceDetails,
                Status = ArtistPackageStatus.Pending
            });
        }

        public async Task UpdateArtistPackageAsync(UpdateArtistPackageRequest updateRequest)
        {

            List<UpdateDefinition<ArtistPackage>> updateDefinition = [];
            UpdateDefinitionBuilder<ArtistPackage> builder = Builders<ArtistPackage>.Update;

            if(!string.IsNullOrEmpty(updateRequest.PackageName))
            {
                updateDefinition.Add(builder.Set(ap => ap.PackageName, updateRequest.PackageName));
            }
            if(updateRequest.Price > 0)
            {
                updateDefinition.Add(builder.Set(ap => ap.Price, updateRequest.Price));
            }
            if(updateRequest.EstimateDeliveryDays > 0)
            {
                updateDefinition.Add(builder.Set(ap => ap.EstimateDeliveryDays, updateRequest.EstimateDeliveryDays));
            }
            if(!string.IsNullOrEmpty(updateRequest.Description))
            {
                updateDefinition.Add(builder.Set(ap => ap.Description, updateRequest.Description));
            }
            if(!string.IsNullOrEmpty(updateRequest.ServiceDetails))
            {
                updateDefinition.Add(builder.Set(ap => ap.ServiceDetails, updateRequest.ServiceDetails));
            }

            UpdateDefinition<ArtistPackage> combinedUpdate = builder.Combine(updateDefinition);

            var result = await _unitOfWork.GetCollection<ArtistPackage>().UpdateOneAsync(ap => ap.Id == updateRequest.Id, combinedUpdate);

            if(result.MatchedCount == 0)
            {
                throw new Exception("Artist package not found.");
            }
            if(result.ModifiedCount == 0)
            {
                throw new Exception("No changes were made to the artist package.");
            }
        }

        public async Task ChangeArtistPackageStatus(UpdateStatusArtistPackageRequest updateStatusRequest)
        {

            var artistPackage = await _unitOfWork.GetCollection<ArtistPackage>()
                                                   .Find(ap => ap.Id == updateStatusRequest.Id)
                                                   .Project<ArtistPackage>(Builders<ArtistPackage>.Projection.Include(ap => ap.Status))
                                                   .FirstOrDefaultAsync();

            if (artistPackage.Status == ArtistPackageStatus.Pending || artistPackage.Status == ArtistPackageStatus.Rejected || artistPackage.Status == ArtistPackageStatus.Canceled)
            {
                throw new Exception("Unavailable for updating this artist package.");
            }

            UpdateDefinition<ArtistPackage> updateDefinition = Builders<ArtistPackage>.Update.Set(ap => ap.Status, updateStatusRequest.Status);

            var result = await _unitOfWork.GetCollection<ArtistPackage>().UpdateOneAsync(ap => ap.Id == updateStatusRequest.Id, updateDefinition);

            if(result.MatchedCount == 0)
            {
                throw new Exception("Artist package not found.");
            }
            if(result.ModifiedCount == 0)
            {
                throw new Exception("No changes were made to the artist package status.");
            }
        }

        public async Task ApproveArtistPackage(UpdateStatusArtistPackageRequest updateStatusRequest)
        {
            UpdateDefinition<ArtistPackage> updateDefinition = Builders<ArtistPackage>.Update.Set(ap => ap.Status, updateStatusRequest.Status);

            var result = await _unitOfWork.GetCollection<ArtistPackage>().UpdateOneAsync(ap => ap.Id == updateStatusRequest.Id, updateDefinition);

            if (result.MatchedCount == 0)
            {
                throw new Exception("Artist package not found.");
            }
            if (result.ModifiedCount == 0)
            {
                throw new Exception("No changes were made to the artist package status.");
            }
        }
    }
}
