using MongoDB.Driver;
using System.Linq.Expressions;

namespace EntityMongowork.Commands
{
    public class MongoCommand
    {
        private MongoCommand(Type entityType, string collectionName, Expression<Func<IMongoDatabase, Task>> expression)
        {
            EntityType = entityType;
            CollectionName = collectionName;
            Expression = expression;
        }

        public Type EntityType { get; set; }
        public string CollectionName { get; set; }
        public Expression<Func<IMongoDatabase, Task>> Expression { get; set; }

        public static MongoCommand FromEntity<T>(string collectionName, Expression<Func<IMongoDatabase, Task>> expression)
        {
            return new MongoCommand(typeof(T), collectionName, expression);
        }
    }
}
