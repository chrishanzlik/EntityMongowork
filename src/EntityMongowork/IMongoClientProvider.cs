using MongoDB.Driver;

namespace EntityMongowork
{
    public interface IMongoClientProvider
    {
        MongoClient ProvideClient();
    }
}
