using EkofyApp.Application.Models.Playlists;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Playlists;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
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
}
