using Megdan.Domain.Entity;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Megdan.Repository
{
    public interface IRepository<T> where T : BaseEntity
    {
        T Insert(T entity);
        T Update(T entity);
        T Delete(T entity);

        E? Get<E>(Expression<Func<T, E>> selector,
            Expression<Func<T, bool>>? predicate = null, //where
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null); //eager loading 

        IEnumerable<E> GetAll<E>(Expression<Func<T, E>> selector,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null);
    }
}
