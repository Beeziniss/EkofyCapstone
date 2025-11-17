using AutoMapper;
using EkofyApp.Application.Models.Conversations;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Chat;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using Stripe.Forwarding;

namespace EkofyApp.Infrastructure.Services.Chat;

public sealed class ChatService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor) : IChatService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

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

    public async Task<string> AddConversationGeneralAsync(string otherUserId)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        List<string> userIds = [userId, otherUserId];

        //check xem da co conversation cua 2 nguoi nay chua
        string conversationId = await _unitOfWork.GetCollection<Conversation>()
            .Find(c => userIds.All(id => c.UserIds.Contains(id)) && c.Status == ConversationStatus.None && c.RequestHubId == null)
            .Project(x => x.Id)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrEmpty(conversationId))
        {
            return conversationId;
        }

        //chua co thi tao moi conversation
        string newConversationId = ObjectId.GenerateNewId().ToString();
        await _unitOfWork.GetCollection<Conversation>().InsertOneAsync(new()
        {
            Id = newConversationId,
            UserIds = userIds,
            Status = ConversationStatus.None,
        });

        return newConversationId;
    }

    public async Task<string> AddConversationFromRequestHubAsync(CreateConversationRequest request)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        List<string> userIds = [userId, request.OtherUserId];

        //check xem da co conversation cua 2 nguoi nay chua
        string conversationId = await _unitOfWork.GetCollection<Conversation>()
            .Find(c => userIds.All(id => c.UserIds.Contains(id)) && c.Status != ConversationStatus.None &&
                       request.RequestHubId == c.RequestHubId)
            .Project(x => x.Id)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrEmpty(conversationId))
        {
            return conversationId;
        }

        //chua co thi tao moi conversation
        Conversation conversation = new()
        {
            UserIds = userIds,
            RequestHubId = request.RequestHubId,
            Status = ConversationStatus.Pending
        };
        await _unitOfWork.GetCollection<Conversation>().InsertOneAsync(conversation);

        return conversation.Id;
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
