using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JamCreator.Shared.Models;
using JamCreator.Shared.Interfaces;
using System.Linq.Expressions;

namespace JamCreator.Data
{
    public class Repository<T, TKey> : IRepository<T, TKey> where T : class, IEntity<TKey>
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _db;

        public Repository(AppDbContext context)
        {
            _context = context;
            _db = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(TKey id, CancellationToken ct = default)
        {
            try
            {
                return await _db.FirstOrDefaultAsync(e => e.Id!.Equals(id), ct);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Failed to fetch entity by ID.", ex);
            }
        }

        public async Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        {
            try
            {
                IQueryable<T> q = _db.AsNoTracking();
                if (predicate is not null) q = q.Where(predicate);
                return await q.ToListAsync(ct);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Failed to fetch entity list.", ex);
            }
        }

        public async Task AddAsync(T entity, CancellationToken ct = default)
        {
            try
            {
                await _db.AddAsync(entity, ct);
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Failed to add entity.", ex);
            }
        }

        public async Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            try
            {
                _db.Update(entity);
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Failed to update entity.", ex);
            }
        }

        public async Task DeleteAsync(T entity, CancellationToken ct = default)
        {
            try
            {
                _db.Remove(entity);
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Failed to delete entity.", ex);
            }
        }

        public async Task<bool> DeleteByIdAsync(TKey id, CancellationToken ct = default)
        {
            try
            {
                var e = await GetByIdAsync(id, ct);
                if (e is null) return false;

                _db.Remove(e);
                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Failed to delete entity by ID.", ex);
            }
        }
    }
}
