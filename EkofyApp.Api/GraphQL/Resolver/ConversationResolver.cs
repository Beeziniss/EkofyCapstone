using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Conversation))]
public sealed class ConversationResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting<User>]
    public IQueryable<User> GetOwnerUserConversationAsync([Parent] Conversation conversation, [Service] IUnitOfWork unitOfWork, [Service] IHttpContextAccessor httpContextAccessor)
    {
        string userId = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("Your session is limit");

        IEnumerable<string> userIds = [userId];

        IEnumerable<string> intersectList = conversation.UserIds.Intersect(userIds);

        return unitOfWork.GetCollection<User>().Find(x => intersectList.Contains(x.Id)).ToEnumerable().AsQueryable();
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting<User>]
    public IQueryable<User> GetOtherUserConversationAsync([Parent] Conversation conversation, [Service] IUnitOfWork unitOfWork, [Service] IHttpContextAccessor httpContextAccessor)
    {
        string userId = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("Your session is limit");

        IEnumerable<string> otherUserIds = conversation.UserIds.Where(id => id != userId).Distinct();

        return unitOfWork.GetCollection<User>().Find(x => otherUserIds.Contains(x.Id)).ToEnumerable().AsQueryable();
    }
}
