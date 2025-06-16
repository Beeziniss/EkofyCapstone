using MongoDB.Driver;

namespace HealthyNutritionApp.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IMongoCollection<TDocument> GetCollection<TDocument>() where TDocument : class;
    }
}
