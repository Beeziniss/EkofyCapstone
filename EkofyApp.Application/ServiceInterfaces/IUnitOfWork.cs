using MongoDB.Driver;

namespace EkofyApp.Application.ServiceInterfaces;

public interface IUnitOfWork : IDisposable
{
    IMongoCollection<TDocument> GetCollection<TDocument>() where TDocument : class;
}
