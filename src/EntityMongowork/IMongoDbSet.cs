using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EntityMongowork
{
    public interface IMongoDbSet
    {
        internal IReadOnlyCollection<object> ModifiedEntities { get; }
        internal void ClearModifedEntities();
    }

    public interface IMongoDbSet<T> : IMongoDbSet where T : class
    {
        Task<T> GetByIdAsync(object id, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>>? expression = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetByFilterAsync(FilterDefinition<T> definition, FindOptions<T, T>? options = null, CancellationToken cancellationToken = default);
        void Update(object id, T item, ReplaceOptions? replaceOptions = null, CancellationToken cancellationToken = default);
        void Insert(T item, InsertOneOptions? insertOptions = null, CancellationToken cancellationToken = default);
        void Remove(object id, DeleteOptions? deleteOptions = null, CancellationToken cancellationToken = default);
    }
}
