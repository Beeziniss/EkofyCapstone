using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums.Reports;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using HotChocolate.Data;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Report))]
public sealed class ReportResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUserReporter([Parent] Report report, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => x.Id == report.ReporterId);
    }

    public async Task<string> GetNicknameReporter([Parent] Report report, [Service] IUnitOfWork unitOfWork)
    {
        if (unitOfWork.GetCollection<User>().Find(x => x.Id == report.ReporterId).Project(x => x.Role).FirstOrDefault() == UserRole.Listener)
        {
            return await unitOfWork.GetCollection<Listener>().Find(x => x.UserId == report.ReporterId).Project(x => x.DisplayName).FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found user {report.ReporterId}");
        }
        else
        {
            return await unitOfWork.GetCollection<Artist>().Find(x => x.UserId == report.ReporterId).Project(x => x.StageName).FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found user {report.ReporterId}");
        }
    }

    public async Task<string> GetNicknameReported([Parent] Report report, [Service] IUnitOfWork unitOfWork)
    {
        if (unitOfWork.GetCollection<User>().Find(x => x.Id == report.ReportedUserId).Project(x => x.Role).FirstOrDefault() == UserRole.Listener)
        {
            return await unitOfWork.GetCollection<Listener>().Find(x => x.UserId == report.ReportedUserId).Project(x => x.DisplayName).FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found user {report.ReportedUserId}");
        }
        else
        {
            return await unitOfWork.GetCollection<Artist>().Find(x => x.UserId == report.ReportedUserId).Project(x => x.StageName).FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found user {report.ReportedUserId}");
        }
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUserReported([Parent] Report report, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => x.Id == report.ReportedUserId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUserAssignedTo([Parent] Report report, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => x.Id == report.AssignedModeratorId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Track> GetTrack([Parent] Report report, [Service] IUnitOfWork unitOfWork)
    {
        if (report.RelatedContentType != ReportRelatedContentType.Track)
        {
            return Enumerable.Empty<Track>().AsQueryable();
        }

        return unitOfWork.GetCollection<Track>().AsQueryable().Where(x => x.Id == report.RelatedContentId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Comment> GetComment([Parent] Report report, [Service] IUnitOfWork unitOfWork)
    {
        if (report.RelatedContentType != ReportRelatedContentType.Comment)
        {
            return Enumerable.Empty<Comment>().AsQueryable();
        }

        return unitOfWork.GetCollection<Comment>().AsQueryable().Where(x => x.Id == report.RelatedContentId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Request> GetRequest([Parent] Report report, [Service] IUnitOfWork unitOfWork)
    {
        if (report.RelatedContentType != ReportRelatedContentType.Request)
        {
            return Enumerable.Empty<Request>().AsQueryable();
        }

        return unitOfWork.GetCollection<Request>().AsQueryable().Where(x => x.Id == report.RelatedContentId);
    }
}
