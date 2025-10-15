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

    public IQueryable<Playlist> GetPlaylists()
    {
        return _unitOfWork.GetCollection<Playlist>().AsQueryable();
    }

    public IQueryable<Playlist> SearchPlaylists(string name)
    {
        IQueryable<Playlist> query = _unitOfWork.GetCollection<Playlist>().AsQueryable();

        if (string.IsNullOrEmpty(name))
        {
            return query;
        }

        string unsignedSearchTerm = HelperMethod.ToUnsigned(name);
        query = query.Where(t => t.NameUnsigned.Contains(unsignedSearchTerm));

        return query;
    }

    public async Task CreatePlaylistAsync(CreatePlaylistRequest createPlaylistRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        await _unitOfWork.GetCollection<Playlist>().InsertOneAsync(new Playlist()
        {
            UserId = userId,
            Name = createPlaylistRequest.Name,
            NameUnsigned = HelperMethod.ToUnsigned(createPlaylistRequest.Name),
            Description = createPlaylistRequest.Description,
            CoverImage = createPlaylistRequest.CoverImage,
            IsPublic = createPlaylistRequest.IsPublic,
        });
    }

    public async Task UpdatePlaylistAsync(UpdatePlaylistRequest updatePlaylistRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Build update definition based on provided fields
        UpdateDefinitionBuilder<Playlist> updateDefinitionBuilder = Builders<Playlist>.Update;
        List<UpdateDefinition<Playlist>> updates = [];

        if (!string.IsNullOrEmpty(updatePlaylistRequest.Name))
        {
            updates.Add(updateDefinitionBuilder.Set(x => x.Name, updatePlaylistRequest.Name));
            updates.Add(updateDefinitionBuilder.Set(x => x.NameUnsigned, HelperMethod.ToUnsigned(updatePlaylistRequest.Name)));
        }

        if (updatePlaylistRequest.Description != null)
        {
            updates.Add(updateDefinitionBuilder.Set(x => x.Description, updatePlaylistRequest.Description));
        }

        if (updatePlaylistRequest.CoverImage != null)
        {
            updates.Add(updateDefinitionBuilder.Set(x => x.CoverImage, updatePlaylistRequest.CoverImage));
        }

        if (updatePlaylistRequest.IsPublic.HasValue)
        {
            updates.Add(updateDefinitionBuilder.Set(x => x.IsPublic, updatePlaylistRequest.IsPublic.Value));
        }

        updates.Add(updateDefinitionBuilder.Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));

        if (updates.Count == 1) // Only UpdatedAt
        {
            throw new BadRequestCustomException("No fields to update.");
        }

        UpdateDefinition<Playlist> updateDefinition = updateDefinitionBuilder.Combine(updates);

        // Update only if the playlist belongs to the user
        UpdateResult updateResult = await _unitOfWork.GetCollection<Playlist>()
            .UpdateOneAsync(x => x.Id == updatePlaylistRequest.PlaylistId && x.UserId == userId, updateDefinition);

        if (updateResult.MatchedCount == 0)
        {
            throw new NotFoundCustomException("Playlist not found");
        }

        if (updateResult.ModifiedCount < updates.Count)
        {
            throw new UnprocessableEntityCustomException("Cannot update playlist");
        }
    }

    public async Task AddToPlaylistAsync(AddToPlaylistRequest addToPlaylistRequest)
    {
        string listenerId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        Playlist? playlist = await _unitOfWork.GetCollection<Playlist>()
            .Find(x => x.Id == addToPlaylistRequest.PlaylistId)
            .Project<Playlist>(Builders<Playlist>.Projection
                .Include(x => x.Id)
                .Include(x => x.TracksInfo))
            .FirstOrDefaultAsync();

        if (playlist == null)
        {
            await _unitOfWork.GetCollection<Playlist>().InsertOneAsync(new Playlist()
            {
                UserId = listenerId,
                Name = addToPlaylistRequest.PlaylistName!,
                NameUnsigned = HelperMethod.ToUnsigned(addToPlaylistRequest.PlaylistName!),
                TracksInfo =
                [
                    new PlaylistTracksInfo
                    {
                        TrackId = addToPlaylistRequest.TrackId,
                        AddedTime = HelperMethod.GetUtcPlus7TimeOffset(),
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
                AddedTime = HelperMethod.GetUtcPlus7TimeOffset(),
            });
        UpdateResult updateResult = await _unitOfWork.GetCollection<Playlist>()
            .UpdateOneAsync(x => x.Id == playlist.Id, updateDefinition);
    }

    public async Task AddToFavoriteAsync(AddToPlaylistRequest addToPlaylistRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        Playlist? favoritePlaylist = await _unitOfWork.GetCollection<Playlist>()
            .Find(x => x.UserId == userId && x.Name == "Favorite Songs")
            .Project<Playlist>(Builders<Playlist>.Projection
                .Include(x => x.Id)
                .Include(x => x.TracksInfo))
            .FirstOrDefaultAsync();

        if (favoritePlaylist == null)
        {
            await _unitOfWork.GetCollection<Playlist>().InsertOneAsync(new Playlist()
            {
                UserId = userId,
                Name = "Favorite Songs",
                NameUnsigned = HelperMethod.ToUnsigned("Favorite Songs"),
                TracksInfo =
                [
                    new PlaylistTracksInfo
                    {
                        TrackId = addToPlaylistRequest.TrackId,
                        AddedTime = HelperMethod.GetUtcPlus7TimeOffset(),
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
                AddedTime = HelperMethod.GetUtcPlus7TimeOffset(),
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
        DeleteResult deleteResult = await _unitOfWork.GetCollection<Playlist>()
            .DeleteOneAsync(x => x.Id == playlistId);

        if (deleteResult.DeletedCount == 0)
        {
            throw new NotFoundCustomException("Playlist does not exist.");
        }
    }
}
