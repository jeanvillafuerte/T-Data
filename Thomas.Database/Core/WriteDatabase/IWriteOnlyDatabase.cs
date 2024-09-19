using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Thomas.Database.Core.WriteDatabase
{
    public interface IWriteOnlyDatabase
    {
        void Insert<T>(T entity);
        TE Insert<T, TE>(T entity);
        void Update<T>(T entity);
        void Delete<T>(T entity);
        void Truncate<T>(bool forceResetAutoIncrement = false);
        void Truncate(string tableName, bool forceResetAutoIncrement = false);
        void UpdateIf<T>(Expression<Func<T, bool>> condition, Expression<Func<T, object>> updateField, object newValue);
        void UpdateIf<T>(Expression<Func<T, bool>> condition, List<(Expression<Func<T, object>> updateField, object newValue)> updates);
        void DeleteIf<T>(Expression<Func<T, bool>> condition);
    }
}
