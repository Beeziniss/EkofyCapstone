using AutoMapper;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Chat;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Chat;
public sealed class ChatGraphQLService(IUnitOfWork unitOfWork, IMapper mapper) : IChatGraphQLService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;

    public IQueryable<Message> GetMessagesExecutable()
    {
        return _unitOfWork.GetCollection<Message>().AsQueryable();
    }
}
