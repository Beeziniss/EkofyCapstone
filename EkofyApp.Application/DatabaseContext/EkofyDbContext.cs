using MongoDB.Driver;

namespace EkofyApp.Application.DatabaseContext
{
    public class EkofyDbContext(MongoDbSetting mongoDBSettings, IMongoClient mongoClient)
    {

        private readonly IMongoDatabase _database = mongoClient.GetDatabase(mongoDBSettings.DatabaseName);

        public IMongoDatabase GetDatabase()
        {
            return _database;
        }
    }

}
