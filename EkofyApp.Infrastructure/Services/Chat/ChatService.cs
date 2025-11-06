using AutoMapper;
using EkofyApp.Application.Models.Conversations;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Chat;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Chat;
public sealed class ChatService(IUnitOfWork unitOfWork, IMapper mapper) : IChatService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;

    public IQueryable<Message> GetMessages()
    {
        return _unitOfWork.GetCollection<Message>().AsQueryable();
    }

    public IQueryable<Conversation> GetConversations()
    {
        return _unitOfWork.GetCollection<Conversation>().AsQueryable();
    }

    public IQueryable<Conversation> GetConversationsByUserId(string userId)
    {
        return _unitOfWork.GetCollection<Conversation>()
            .Find(x => x.UserIds.Contains(userId))
            .ToEnumerable()
            .AsQueryable();
    }

    public async Task AddConversationFromRequestHubAsync(CreateConversationRequest request)
    {
        //check xem da co conversation cua 2 nguoi nay chua
        var isExistingConversation = await _unitOfWork.GetCollection<Conversation>()
            .Find(c => request.UserIds.All(id => c.UserIds.Contains(id)) &&
                       request.RequestHubId == c.RequestHubId)
            .AnyAsync();
        if (isExistingConversation)
        {
            return;
        }

        //chua co thi tao moi conversation
        var conversation = new Conversation() 
        {
            UserIds = request.UserIds,
            RequestHubId = request.RequestHubId,
            Status = ConversationStatus.Pending
        };
        await _unitOfWork.GetCollection<Conversation>().InsertOneAsync(conversation);
    }

    public async Task UpdateConversationStatusAsync(string conversationId, ConversationStatus status)
    {
        UpdateDefinition<Conversation> update = Builders<Conversation>.Update.Set(c => c.Status, status);

        UpdateResult updateResult = await _unitOfWork.GetCollection<Conversation>()
            .UpdateOneAsync(c => c.Id == conversationId, update);
        if (updateResult.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Failed to update conversation status");
        }
    }
}
