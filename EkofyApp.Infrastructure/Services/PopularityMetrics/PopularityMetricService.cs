using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.PopularityMetrics;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.PopularityMetrics;

public sealed class PopularityMetricService(IUnitOfWork unitOfWork) : IPopularityMetricService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    // Default Score
    private const decimal DefaultPopularityScore = 1.0m;

    // Streaming Metrics
    private const decimal PopularitySkipStreaming = 0.3m;
    private const decimal PopularityStreaming = 0.5m;
    private const decimal PopularityCompleteStreaming = 1.0m;
    private const decimal PopularityRepeatStreaming = 1.2m;

    // Engagement Metrics
    private const decimal PopularityFavorite = 1.0m;
    private const decimal PopularityUnfavorite = -1.0m;
    private const decimal PopularityShare = 1.5m;
    private const decimal PopularityAddToPlaylist = 2.0m;
    private const decimal PopularityRemoveFromPlaylist = -1.0m;
    private const decimal PopularityComment = 1.2m;

    // Discovery Metrics
    private const decimal PopularitySearch = 0.8m;
    private const decimal PopularitySearchResultClick = 1.0m;
    private const decimal PopularityClickFromRecommendation = 1.2m;

    public async Task ProcessTrackStreamingMetricAsync(string trackId, PopularityActionType actionType)
    {
        decimal popularityScore = actionType switch
        {
            PopularityActionType.SkipStreaming => DefaultPopularityScore * PopularitySkipStreaming,
            PopularityActionType.Streaming => DefaultPopularityScore * PopularityStreaming,
            PopularityActionType.CompleteStreaming => DefaultPopularityScore * PopularityCompleteStreaming,
            PopularityActionType.RepeatStreaming => DefaultPopularityScore * PopularityRepeatStreaming,
            _ => 0m
        };

        UpdateResult updateResult = await _unitOfWork.GetCollection<Track>().UpdateOneAsync(x => x.Id == trackId,
            Builders<Track>.Update.Inc(x => x.Popularity, popularityScore));
        if (updateResult.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Failed to update track popularity metric.");
        }
    }

    public async Task ProcessTrackEngagementMetricAsync(string trackId, PopularityActionType actionType)
    {
        decimal popularityScore = actionType switch
        {
            PopularityActionType.Favorite => DefaultPopularityScore * PopularityFavorite,
            PopularityActionType.Unfavorite => DefaultPopularityScore * PopularityUnfavorite,
            PopularityActionType.Share => DefaultPopularityScore * PopularityShare,
            PopularityActionType.AddToPlaylist => DefaultPopularityScore * PopularityAddToPlaylist,
            PopularityActionType.RemoveFromPlaylist => DefaultPopularityScore * PopularityRemoveFromPlaylist,
            PopularityActionType.Comment => DefaultPopularityScore * PopularityComment,
            _ => 0m
        };
        UpdateResult updateResult = await _unitOfWork.GetCollection<Track>().UpdateOneAsync(x => x.Id == trackId,
            Builders<Track>.Update.Inc(x => x.Popularity, popularityScore));
        if (updateResult.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Failed to update track popularity metric.");
        }
    }

    public async Task ProcessTrackDiscoveryMetricAsync(string trackId, PopularityActionType actionType)
    {
        decimal popularityScore = actionType switch
        {
            PopularityActionType.Search => DefaultPopularityScore * PopularitySearch,
            PopularityActionType.SearchResultClick => DefaultPopularityScore * PopularitySearchResultClick,
            PopularityActionType.ClickFromRecommendation => DefaultPopularityScore * PopularityClickFromRecommendation,
            _ => 0m
        };
        UpdateResult updateResult = await _unitOfWork.GetCollection<Track>().UpdateOneAsync(x => x.Id == trackId,
            Builders<Track>.Update.Inc(x => x.Popularity, popularityScore));
        if (updateResult.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Failed to update track popularity metric.");
        }
    }

    public async Task ProcessArtistEngagementMetricAsync(string artistId, PopularityActionType actionType)
    {
        decimal popularityScore = actionType switch
        {
            PopularityActionType.Follow => DefaultPopularityScore * PopularityFavorite,
            PopularityActionType.Unfollow => DefaultPopularityScore * PopularityUnfavorite,
            PopularityActionType.Share => DefaultPopularityScore * PopularityShare,
            _ => 0m
        };

        UpdateResult updateResult = await _unitOfWork.GetCollection<Artist>().UpdateOneAsync(x => x.Id == artistId,
            Builders<Artist>.Update.Inc(x => x.Popularity, popularityScore));
        if (updateResult.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Failed to update artist popularity metric.");
        }
    }

    public async Task ProcessArtistDiscoveryMetricAsync(string artistId, PopularityActionType actionType)
    {
        decimal popularityScore = actionType switch
        {
            PopularityActionType.Search => DefaultPopularityScore * PopularitySearch,
            PopularityActionType.SearchResultClick => DefaultPopularityScore * PopularitySearchResultClick,
            PopularityActionType.ClickFromRecommendation => DefaultPopularityScore * PopularityClickFromRecommendation,
            _ => 0m
        };

        UpdateResult updateResult = await _unitOfWork.GetCollection<Artist>().UpdateOneAsync(x => x.Id == artistId,
            Builders<Artist>.Update.Inc(x => x.Popularity, popularityScore));
        if (updateResult.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Failed to update artist popularity metric.");
        }
    }
}
