using EntityMongowork.Commands;
using MongoDB.Driver;
using System.Reflection;

namespace EntityMongowork
{
    public abstract class MongoDbContext : IMongoDbContext
    {
        private readonly MongoClient _client;
        private readonly IMongoDatabase _db;
        private readonly ICommandStore _commandStore;
        private readonly bool _useTransactions;
        private readonly Dictionary<Type, IMongoDbSet> _dbSets;

        public MongoDbContext(string connectionString, string databaseName, bool useTransactions = false)
            : this(() => new MongoClient(connectionString), databaseName, useTransactions) { }

        public MongoDbContext(IMongoClientProvider clientProvider, string databaseName, bool useTransactions = false)
            : this(clientProvider.ProvideClient, databaseName, useTransactions) { }

        public MongoDbContext(Func<MongoClient> clientProvider, string databaseName, bool useTransactions = false)
        {
            _dbSets = new Dictionary<Type, IMongoDbSet>();
            _client = clientProvider();
            _db = _client.GetDatabase(databaseName);
            _commandStore = new InMemoryCommandStore();
            _useTransactions = useTransactions;

            var props = FindDbSetProperties();
            InitializeDbSets(props);
        }

        protected IReadOnlyCollection<object> ModifiedEntities => _dbSets.Values.SelectMany(x => x.ModifiedEntities).ToList().AsReadOnly();

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


        protected virtual IReadOnlyCollection<PropertyInfo> FindDbSetProperties()
        {
            return this.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(m =>
                    m.PropertyType.IsGenericType &&
                    m.PropertyType.GetGenericTypeDefinition() == typeof(MongoDbSet<>)
                    && (m.GetGetMethod()?.IsVirtual ?? false))
                .ToList()
                .AsReadOnly();
        }

        protected void InitializeDbSets(IReadOnlyCollection<PropertyInfo> propertyInfos)
        {
            foreach(var propertyInfo in propertyInfos)
            {
                var currentValue = propertyInfo.GetValue(this);

                if (currentValue is null)
                {
                    if (CreateDbSet(propertyInfo, _db, _commandStore) is IMongoDbSet set)
                    {
                        _dbSets.Add(propertyInfo.PropertyType, set);
                        propertyInfo.SetValue(this, set);
                    }
                }
            }
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
                var lambda = _commandStore.GetNext()!.Expression.Compile();
                await lambda.Invoke(_db);
            }

            foreach(var dbSet in _dbSets.Values)
            {
                dbSet.ClearModifedEntities();
            }
        }
    }
}
