using EkofyApp.Application.Models.Conversations;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Conversation))]
public sealed class ConversationResolver
{
    public async Task<ConversationResponse> GetOwnerProfileConversationAsync([Parent] Conversation conversation, [Service] IUnitOfWork unitOfWork, [Service] IHttpContextAccessor httpContextAccessor)
    {
        string userId = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("Your session is limit");

        IEnumerable<string> userIds = [userId];

        IEnumerable<string> intersectList = conversation.UserIds.Intersect(userIds);

        UserRole userRole = await unitOfWork.GetCollection<User>()
            .Find(x => intersectList.Contains(x.Id))
            .Project(x => x.Role)
            .FirstOrDefaultAsync();

        if (userRole == UserRole.Listener)
        {
            return await unitOfWork.GetCollection<Listener>()
                .Find(x => intersectList.Contains(x.UserId))
                .Project(x => new ConversationResponse()
                {
                    Nickname = x.DisplayName,
                    Avatar = x.AvatarImage ?? string.Empty
                })
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Listener not found");
        }

        return await unitOfWork.GetCollection<Artist>()
            .Find(x => intersectList.Contains(x.UserId))
            .Project(x => new ConversationResponse()
            {
                Nickname = x.StageName,
                Avatar = x.AvatarImage ?? string.Empty
            })
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Artist not found");
    }

    public async Task<ConversationResponse> GetOtherProfileConversationAsync([Parent] Conversation conversation, [Service] IUnitOfWork unitOfWork, [Service] IHttpContextAccessor httpContextAccessor)
    {
        string userId = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("Your session is limit");

        IEnumerable<string> otherUserIds = conversation.UserIds.Where(id => id != userId).Distinct();

        UserRole userRole = unitOfWork.GetCollection<User>()
            .Find(x => otherUserIds.Contains(x.Id))
            .Project(x => x.Role)
            .FirstOrDefault();

        if (userRole == UserRole.Listener)
        {
            return await unitOfWork.GetCollection<Listener>()
                .Find(x => otherUserIds.Contains(x.UserId))
                .Project(x => new ConversationResponse()
                {
                    Nickname = x.DisplayName,
                    Avatar = x.AvatarImage ?? string.Empty
                })
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Listener not found");
        }

        return await unitOfWork.GetCollection<Artist>()
            .Find(x => otherUserIds.Contains(x.UserId))
            .Project(x => new ConversationResponse()
            {
                Nickname = x.StageName,
                Avatar = x.AvatarImage ?? string.Empty
            })
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Artist not found");
    }
}
