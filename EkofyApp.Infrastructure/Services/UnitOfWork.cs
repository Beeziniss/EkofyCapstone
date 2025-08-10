using EkofyApp.Application.DatabaseContext;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services;
public class UnitOfWork(EkofyDbContext dbContext) : IUnitOfWork
{
    private readonly IMongoDatabase _database = dbContext.GetDatabase();
    private readonly IMongoClient _mongoClient = dbContext.GetMongoClient();
    private IClientSessionHandle? _session = default!;
    public IClientSessionHandle Session => _session ?? throw new TransactionOperationCustomException("Session is not initialized. Call BeginTransactionAsync first.");

    private bool disposedValue;

    public IMongoCollection<TDocument> GetCollection<TDocument>() where TDocument : class
    {
        return _database.GetCollection<TDocument>(typeof(TDocument).Name);
    }

    public async Task BeginTransactionAsync()
    {
        if (_session != null)
        {
            throw new TransactionOperationCustomException("A transaction is already in progress.");
        }

        _session = await _mongoClient.StartSessionAsync();
        _session.StartTransaction();
    }

    public async Task CommitAsync()
    {
        if (_session == null || !_session.IsInTransaction)
        {
            throw new TransactionOperationCustomException("No transaction to commit.");
        }

        await _session.CommitTransactionAsync();
        _session.Dispose();
        _session = null;
    }

    public async Task RollbackAsync()
    {
        if (_session != null && _session.IsInTransaction)
        {
            await _session.AbortTransactionAsync();
            _session.Dispose();
            _session = null;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<IClientSessionHandle, Task> action)
    {
        using IClientSessionHandle session = await _mongoClient.StartSessionAsync();
        session.StartTransaction();

        try
        {
            await action(session);
            await session.CommitTransactionAsync();
        }
        catch
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<IClientSessionHandle, Task<T>> operation)
    {
        using IClientSessionHandle session = await _mongoClient.StartSessionAsync();
        session.StartTransaction();

        try
        {
            T? result = await operation(session);
            await session.CommitTransactionAsync();
            return result;
        }
        catch
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects)
                if (_session != null)
                {
                    if (_session.IsInTransaction)
                    {
                        _session.AbortTransaction();
                    }
                    _session.Dispose();
                    _session = null;
                }
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
