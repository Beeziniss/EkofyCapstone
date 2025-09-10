using MongoDB.Driver;

namespace EkofyApp.Application.ServiceInterfaces;

public interface IUnitOfWork : IDisposable
{
    Task BeginTransactionAsync();
    Task<BulkWriteResult<TDocument>> BulkWriteAsync<TDocument>(IClientSessionHandle? session, IEnumerable<WriteModel<TDocument>> operations, bool isOrdered = true, CancellationToken cancellationToken = default) where TDocument : class;
    Task<BulkWriteResult<TDocument>> BulkWriteAsync<TDocument>(IEnumerable<WriteModel<TDocument>> operations, bool isOrdered = true, CancellationToken cancellationToken = default) where TDocument : class;
    Task CommitAsync();
    Task ExecuteInTransactionAsync(Func<IClientSessionHandle, Task> action);
    Task<T> ExecuteInTransactionAsync<T>(Func<IClientSessionHandle, Task<T>> operation);
    IMongoCollection<TDocument> GetCollection<TDocument>() where TDocument : class;
    Task RollbackAsync();
}
