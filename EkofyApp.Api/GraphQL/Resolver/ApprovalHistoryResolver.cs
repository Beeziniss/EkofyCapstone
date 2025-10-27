using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(ApprovalHistory))]
public sealed class ApprovalHistoryResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUser([Parent] ApprovalHistory approvalHistory, [Service] IUnitOfWork unitOfWork)
    {
        if(approvalHistory.ApprovalType != ApprovalType.ArtistRegistration)
        {
            return Enumerable.Empty<User>().AsQueryable();
        }

        return unitOfWork.GetCollection<User>().AsQueryable().Where(u => u.Id == approvalHistory.TargetId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Track> GetTrack([Parent] ApprovalHistory approvalHistory, [Service] IUnitOfWork unitOfWork)
    {
        if(approvalHistory.ApprovalType != ApprovalType.TrackUpload)
        {
            return Enumerable.Empty<Track>().AsQueryable();
        }

        return unitOfWork.GetCollection<Track>().AsQueryable().Where(t => t.Id == approvalHistory.TargetId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetApprovedBy([Parent] ApprovalHistory approvalHistory, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(u => u.Id == approvalHistory.ApprovedBy);
    }
}
