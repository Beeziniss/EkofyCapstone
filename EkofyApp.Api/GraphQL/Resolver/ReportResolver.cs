using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums.Reports;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Report))]
public sealed class ReportResolver : ObjectTypeExtension<Report>
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUserReporter([Parent] Report report, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => x.Id == report.ReporterId);
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
    public IQueryable<Track> GetTrack([Parent] Report report, [Service] IUnitOfWork unitOfWork)
    {
        if(report.RelatedContentType != ReportRelatedContentType.Track)
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

    // TODO: Request hub
}
