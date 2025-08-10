using MongoDB.Driver;

namespace EkofyApp.Application.ServiceInterfaces;

public interface IUnitOfWork : IDisposable
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task ExecuteInTransactionAsync(Func<IClientSessionHandle, Task> action);
    Task<T> ExecuteInTransactionAsync<T>(Func<IClientSessionHandle, Task<T>> operation);
    IMongoCollection<TDocument> GetCollection<TDocument>() where TDocument : class;
    Task RollbackAsync();
}
