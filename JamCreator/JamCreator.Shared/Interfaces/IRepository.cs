using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace JamCreator.Shared.Interfaces
{
    public interface IRepository<T, TKey> where T : class, IEntity<TKey>
    {
        Task<T?> GetByIdAsync(TKey id, CancellationToken ct = default);
        Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
        Task AddAsync(T entity, CancellationToken ct = default);
        Task UpdateAsync(T entity, CancellationToken ct = default);
        Task DeleteAsync(T entity, CancellationToken ct = default);
        Task<bool> DeleteByIdAsync(TKey id, CancellationToken ct = default);
    }
}