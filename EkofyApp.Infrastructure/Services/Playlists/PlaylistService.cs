using EkofyApp.Application.Models.Playlists;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Playlists;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Playlists;
public sealed class PlaylistService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IPlaylistService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public IQueryable<Playlist> GetPlaylistsQueryable()
    {
        return _unitOfWork.GetCollection<Playlist>().AsQueryable();
    }

    public async Task CreatePlaylistAsync(CreatePlaylistRequest createPlaylistRequest)
    {
        string listenerId = _httpContextAccessor.HttpContext?.User.FindFirst("listenerId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        await _unitOfWork.GetCollection<Playlist>().InsertOneAsync(new Playlist()
        {
            ListenerId = listenerId,
            Name = createPlaylistRequest.Name,
            Description = createPlaylistRequest.Description,
            CoverImage = createPlaylistRequest.CoverImage,
            IsPublic = createPlaylistRequest.IsPublic,
        });
    }

    public async Task AddToPlaylistAsync(AddToPlaylistRequest addToPlaylistRequest)
    {
        string listenerId = _httpContextAccessor.HttpContext?.User.FindFirst("listenerId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        Playlist? playlist = await _unitOfWork.GetCollection<Playlist>()
            .Find(x => x.Id == addToPlaylistRequest.PlaylistId)
            .FirstOrDefaultAsync();

        if (playlist == null)
        {
            await _unitOfWork.GetCollection<Playlist>().InsertOneAsync(new Playlist()
            {
                ListenerId = listenerId,
                Name = addToPlaylistRequest.PlaylistName!,
                TracksInfo =
                [
                    new PlaylistTracksInfo
                    {
                        TrackId = addToPlaylistRequest.TrackId,
                        AddedTime = HelperMethod.GetUtcPlus7Time(),
                    }
                ]
            });

            return;
        }

        if (playlist.TracksInfo.Any(x => x.TrackId == addToPlaylistRequest.TrackId))
        {
            throw new BadRequestCustomException("Track already added in the playlist.");
        }

        UpdateDefinition<Playlist> updateDefinition = Builders<Playlist>.Update
            .Push(x => x.TracksInfo, new PlaylistTracksInfo
            {
                TrackId = addToPlaylistRequest.TrackId,
                AddedTime = HelperMethod.GetUtcPlus7Time(),
            });
        UpdateResult updateResult = await _unitOfWork.GetCollection<Playlist>()
            .UpdateOneAsync(x => x.Id == playlist.Id, updateDefinition);
    }

    public async Task AddToFavoriteAsync(AddToPlaylistRequest addToPlaylistRequest)
    {
        string listenerId = _httpContextAccessor.HttpContext?.User.FindFirst("listenerId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        Playlist? favoritePlaylist = await _unitOfWork.GetCollection<Playlist>()
            .Find(x => x.ListenerId == listenerId && x.Name == "Favorite Songs")
            .FirstOrDefaultAsync();

        if (favoritePlaylist == null)
        {
            await _unitOfWork.GetCollection<Playlist>().InsertOneAsync(new Playlist()
            {
                ListenerId = listenerId,
                Name = "Favorite Songs",
                TracksInfo =
                [
                    new PlaylistTracksInfo
                    {
                        TrackId = addToPlaylistRequest.TrackId,
                        AddedTime = HelperMethod.GetUtcPlus7Time(),
                    }
                ]
            });

            return;
        }

        if (favoritePlaylist.TracksInfo.Any(x => x.TrackId == addToPlaylistRequest.TrackId))
        {
            throw new BadRequestCustomException("Track already added in the favorite playlist.");
        }

        UpdateDefinition<Playlist> updateDefinition = Builders<Playlist>.Update
            .Push(x => x.TracksInfo, new PlaylistTracksInfo
            {
                TrackId = addToPlaylistRequest.TrackId,
                AddedTime = HelperMethod.GetUtcPlus7Time(),
            });
        UpdateResult updateResult = await _unitOfWork.GetCollection<Playlist>().UpdateOneAsync(x => x.Id == favoritePlaylist.Id, updateDefinition);
    }

    public async Task RemoveFromPlaylistAsync(AddToPlaylistRequest addToPlaylistRequest)
    {
        UpdateDefinition<Playlist> updateDefinition = Builders<Playlist>.Update
            .PullFilter(x => x.TracksInfo, y => y.TrackId == addToPlaylistRequest.TrackId);
        UpdateResult updateResult = await _unitOfWork.GetCollection<Playlist>()
            .UpdateOneAsync(x => x.Id == addToPlaylistRequest.PlaylistId, updateDefinition);

        if (updateResult.ModifiedCount == 0)
        {
            throw new BadRequestCustomException("Track does not exist in the playlist.");
        }
    }

    public async Task DeletePlaylistAsync(string playlistId)
    {
        string listenerId = _httpContextAccessor.HttpContext?.User.FindFirst("listenerId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        DeleteResult deleteResult = await _unitOfWork.GetCollection<Playlist>()
            .DeleteOneAsync(x => x.Id == playlistId);

        if (deleteResult.DeletedCount == 0)
        {
            throw new NotFoundCustomException("Playlist does not exist.");
        }
    }
}
