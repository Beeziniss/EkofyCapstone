using EkofyApp.Application.DatabaseContext;
using EkofyApp.Application.ServiceInterfaces;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services;
public class UnitOfWork(EkofyDbContext dbContext) : IUnitOfWork
{
    private readonly IMongoDatabase _database = dbContext.GetDatabase();
    private bool disposedValue;

    public IMongoCollection<TDocument> GetCollection<TDocument>() where TDocument : class
    {
        return _database.GetCollection<TDocument>(typeof(TDocument).Name);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
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
