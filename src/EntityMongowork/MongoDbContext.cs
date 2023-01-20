using EntityMongowork.Commands;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using System.Reflection;

namespace EntityMongowork
{
    public abstract class MongoDbContext : IMongoDbContext
    {
        private readonly MongoClient _client;
        private readonly IMongoDatabase _db;
        private readonly ICommandStore _commandStore;
        private readonly bool _useTransactions;

        public MongoDbContext(string connectionString, string databaseName, bool useTransactions = false)
        {
            _client = new MongoClient(connectionString);
            _db = _client.GetDatabase(databaseName);
            _commandStore = new InMemoryCommandStore();
            _useTransactions = useTransactions;

            InitializeDbSets();
        }

        public MongoDbContext(IMongoClientProvider clientProvider, string databaseName, bool useTransactions = false)
        {
            _client = clientProvider.ProvideClient();
            _db = _client.GetDatabase(databaseName);
            _commandStore = new InMemoryCommandStore();
            _useTransactions = useTransactions;

            InitializeDbSets();
        }

        public MongoDbContext(Func<MongoClient> clientProvider, string databaseName, bool useTransactions = false)
        {
            _client = clientProvider();
            _db = _client.GetDatabase(databaseName);
            _commandStore = new InMemoryCommandStore();
            _useTransactions = useTransactions;

            InitializeDbSets();
        }

        /// <inheritdoc />
        public virtual async Task SaveChangesAsync()
        {
            if (!_useTransactions)
            {
                await SaveChangesInternalAsync();
            }
            else
            {
                using var handle = await _client.StartSessionAsync();
                await SaveChangesInternalAsync();
                await handle.CommitTransactionAsync();
            }
        }

        private void InitializeDbSets()
        {
            this.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(m =>
                    m.PropertyType.IsGenericType &&
                    m.PropertyType.GetGenericTypeDefinition() == typeof(MongoDbSet<>)
                    && (m.GetGetMethod()?.IsVirtual ?? false))
                .ToList()
                .ForEach(prop => prop.SetValue(this, CreateDbSet(prop, _db, _commandStore)));
        }

        private static object? CreateDbSet(PropertyInfo prop, IMongoDatabase dbRef, ICommandStore storeRef)
        {
            return prop.PropertyType
                .GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
                    null,
                    new[] { typeof(string), typeof(IMongoDatabase), typeof(ICommandStore), typeof(MongoCollectionSettings) },
                    null)
                ?.Invoke(new object[] { prop.Name, dbRef, storeRef, null! });
        }

        private async Task SaveChangesInternalAsync()
        {
            while (_commandStore.HasNext)
            {
                var lambda = _commandStore.GetNext()!.Expression.Compile() as Func<IMongoDatabase, Task>;
                var task = lambda.Invoke(_db);
                await task;
            }
        }
    }
}
