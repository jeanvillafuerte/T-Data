using System;
using System.Linq.Expressions;

namespace Thomas.Database.Core.WriteDatabase
{
    public interface IWriteOnlyDatabase
    {
        int Update<T>(T entity, Expression<Func<T, object>> where, bool excludeAutogenerateColumns = true) where T : class, new();
        void Add<T>(T entity) where T : class, new();
        TE Add<T, TE>(T entity) where T : class, new();
        int Delete<T>(Expression<Func<T, object>> where) where T : class, new();
    }
}
