namespace EkofyApp.Application.DatabaseContext
{
    public class MongoDbSetting
    {
        public string ConnectionString { get; set; } = default!;
        public string DatabaseName { get; set; } = default!;
    }
}
