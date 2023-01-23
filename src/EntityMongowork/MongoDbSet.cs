using EntityMongowork.Commands;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace EntityMongowork
{
    public class MongoDbSet<T> : IMongoDbSet<T> where T : class
    {
        private readonly IMongoCollection<T> _collection;
        private readonly ICommandStore _commands;
        private readonly MongoCollectionSettings _collectionSettings;
        private readonly string _collectionName;
        private readonly List<T> _modifiedEntities;

        IReadOnlyCollection<object> IMongoDbSet.ModifiedEntities => _modifiedEntities.ToList().AsReadOnly();

        internal MongoDbSet(string collectionName, IMongoDatabase database, ICommandStore commandStore, MongoCollectionSettings collectionSettings)
        {
            _modifiedEntities = new List<T>();
            _collectionSettings = collectionSettings;
            _collectionName = collectionName;
            _collection = database.GetCollection<T>(collectionName, collectionSettings);
            _commands = commandStore;
        }

        /// <inheritdoc />
        public async Task<T> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            var cursor = await _collection.FindAsync(Builders<T>.Filter.Eq("_id", id), cancellationToken: cancellationToken);
            return await cursor.SingleOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var cursor = await _collection.FindAsync(Builders<T>.Filter.Empty, cancellationToken: cancellationToken);
            return await cursor.ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>>? expression = null, CancellationToken cancellationToken = default)
        {
            IAsyncCursor<T>? cursor;

            if (expression == null)
            {
                cursor = await _collection.FindAsync(Builders<T>.Filter.Empty, cancellationToken: cancellationToken);
            }
            else
            {
                cursor = await _collection.FindAsync(Builders<T>.Filter.Where(expression), cancellationToken: cancellationToken);
            }

            return await cursor.FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<T>> GetByFilterAsync(FilterDefinition<T> definition, FindOptions<T, T>? options = null, CancellationToken cancellationToken = default)
        {
            var cursor = await _collection.FindAsync(definition, options, cancellationToken: cancellationToken);
            return await cursor.ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public void Update(object id, T item, ReplaceOptions? replaceOptions = null, CancellationToken cancellationToken = default)
        {
            var command = MongoCommand.FromEntity<T>(
                _collectionName,
                (IMongoDatabase db) =>
                    db.GetCollection<T>(_collectionName, _collectionSettings)
                        .ReplaceOneAsync(Builders<T>.Filter.Eq("_id", id), item, replaceOptions, cancellationToken));
            _commands.AddCommand(command);
            MarkAsModified(item);
        }

        /// <inheritdoc />
        public void Insert(T item, InsertOneOptions? insertOptions = null, CancellationToken cancellationToken = default)
        {
            var command = MongoCommand.FromEntity<T>(
                _collectionName,
                (IMongoDatabase db) =>
                    db.GetCollection<T>(_collectionName, _collectionSettings)
                        .InsertOneAsync(item, insertOptions, cancellationToken));
            _commands.AddCommand(command);
            MarkAsModified(item);
        }

        /// <inheritdoc />
        public void Remove(object id, DeleteOptions? deleteOptions = null, CancellationToken cancellationToken = default)
        {
            var command = MongoCommand.FromEntity<T>(
                _collectionName,
                (IMongoDatabase db) =>
                    db.GetCollection<T>(_collectionName, _collectionSettings)
                        .DeleteOneAsync(Builders<T>.Filter.Eq("_id", id), deleteOptions, cancellationToken));
            _commands.AddCommand(command);
        }

        private void MarkAsModified(T entity)
        {
            if (_modifiedEntities.Contains(entity))
            {
                return;
            }

            _modifiedEntities.Add(entity);
        }

        void IMongoDbSet.ClearModifedEntities()
        {
            this._modifiedEntities.Clear();

        }
    }
}
