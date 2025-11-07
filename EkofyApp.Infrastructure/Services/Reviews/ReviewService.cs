using EkofyApp.Application.Models.Reviews;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Reviews;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace EkofyApp.Infrastructure.Services.Reviews;
public sealed class ReviewService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IReviewService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public IQueryable<Review> GetReviews()
    {
        return _unitOfWork.GetCollection<Review>().AsQueryable();
    }

    public async Task<ReviewResponse> GetAverageRatingBaseOnPackageAsync(string packageId)
    {
        IEnumerable<string> packageOrderIds = await _unitOfWork.GetCollection<PackageOrder>()
                .Find(x => x.ArtistPackageId == packageId)
                .Project(x => x.Id)
                .ToListAsync();

        List<int> reviews = await _unitOfWork.GetCollection<Review>()
            .Find(x => packageOrderIds.Contains(x.PackageOrderId) && x.DeletedAt == null)
            .Project(x => x.Rating)
            .ToListAsync();

        if (reviews.Count == 0)
        {
            return new ReviewResponse
            {
                AverageRating = 0,
                TotalReviews = 0
            };
        }

        return new ReviewResponse
        {
            AverageRating = Convert.ToInt32(Math.Round(reviews.Average())),
            TotalReviews = reviews.Count
        };
    }

    public async Task CreateReviewAsync(CreateReviewRequest createReviewRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        if (await _unitOfWork.GetCollection<Review>().Find(x => x.ClientId == userId && x.PackageOrderId == createReviewRequest.PackageOrderId).AnyAsync())
        {
            throw new ConflictCustomException("You have already reviewed this package order");
        }

        await _unitOfWork.GetCollection<Review>().InsertOneAsync(new Review
        {
            PackageOrderId = createReviewRequest.PackageOrderId,
            ClientId = userId,
            Rating = createReviewRequest.Rating,
            Comment = createReviewRequest.Comment
        });
    }

    public async Task UpdateReviewAsync(UpdateReviewRequest updateReviewRequest)
    {
        List<UpdateDefinition<Review>> updates = [Builders<Review>.Update.Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())];

        if (updateReviewRequest.Rating != null)
        {
            updates.Add(Builders<Review>.Update.Set(r => r.Rating, updateReviewRequest.Rating.Value));
        }

        if (updateReviewRequest.Comment != null)
        {
            updates.Add(Builders<Review>.Update.Set(r => r.Comment, updateReviewRequest.Comment));
        }

        UpdateDefinition<Review> updateDefinition = Builders<Review>.Update.Combine(updates);

        UpdateResult updateResult = await _unitOfWork.GetCollection<Review>().UpdateOneAsync(
            r => r.Id == updateReviewRequest.ReviewId,
            updateDefinition
        );
        if (updateResult.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Cannot update review");
        }
    }

    public async Task DeleteReviewHardAsync(string reviewId)
    {
        DeleteResult deleteResult = await _unitOfWork.GetCollection<Review>().DeleteOneAsync(r => r.Id == reviewId);
        if (deleteResult.DeletedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Cannot delete review");
        }
    }

    public async Task DeleteReviewSoftAsync(string reviewId)
    {
        UpdateResult updateResult = await _unitOfWork.GetCollection<Review>().UpdateOneAsync(
            r => r.Id == reviewId,
            Builders<Review>.Update.Set(r => r.DeletedAt, HelperMethod.GetUtcPlus7TimeOffset())
        );
        if (updateResult.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Cannot delete review");
        }
    }

    public async Task<bool> CheckClientReviewedPackageOrderAsync(string clientId, string packageOrderId)
    {
        return await _unitOfWork.GetCollection<Review>()
            .AsQueryable()
            .Where(x => x.ClientId == clientId && x.PackageOrderId == packageOrderId && x.DeletedAt == null)
            .AnyAsync();
    }
}
