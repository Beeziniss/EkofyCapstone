using EkofyApp.Application.Models.Messages;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Message))]
public sealed class MessageResolver
{
    public async Task<MessageResponse> GetSenderProfileMessagesAsync([Parent] Message message, [Service] IUnitOfWork unitOfWork)
    {
        UserRole userRole = await unitOfWork.GetCollection<User>()
            .Find(x => x.Id == message.SenderId)
            .Project(x => x.Role)
            .FirstOrDefaultAsync();

        if (userRole == UserRole.Listener)
        {
            return await unitOfWork.GetCollection<Listener>()
                .Find(x => x.UserId == message.SenderId)
                .Project(x => new MessageResponse()
                {
                    Nickname = x.DisplayName,
                    Avatar = x.AvatarImage ?? string.Empty
                })
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Listener not found");
        }

        return await unitOfWork.GetCollection<Artist>()
            .Find(x => x.UserId == message.SenderId)
            .Project(x => new MessageResponse()
            {
                Nickname = x.StageName,
                Avatar = x.AvatarImage ?? string.Empty
            })
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Artist not found");
    }

    public async Task<MessageResponse> GetReceiverProfileMessagesAsync([Parent] Message message, [Service] IUnitOfWork unitOfWork)
    {
        UserRole userRole = await unitOfWork.GetCollection<User>()
            .Find(x => x.Id == message.ReceiverId)
            .Project(x => x.Role)
            .FirstOrDefaultAsync();

        if (userRole == UserRole.Listener)
        {
            return await unitOfWork.GetCollection<Listener>()
                .Find(x => x.UserId == message.ReceiverId)
                .Project(x => new MessageResponse()
                {
                    Nickname = x.DisplayName,
                    Avatar = x.AvatarImage ?? string.Empty
                })
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Listener not found");
        }
        return await unitOfWork.GetCollection<Artist>()
            .Find(x => x.UserId == message.ReceiverId)
            .Project(x => new MessageResponse()
            {
                Nickname = x.StageName,
                Avatar = x.AvatarImage ?? string.Empty
            })
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Artist not found");
    }
}
