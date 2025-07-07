using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Chat;
public interface IChatGraphQLService
{
    IQueryable<Message> GetMessagesExecutable();
}
