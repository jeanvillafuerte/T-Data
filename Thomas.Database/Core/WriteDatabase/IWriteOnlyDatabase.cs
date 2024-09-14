using System;
using System.Linq.Expressions;

namespace Thomas.Database.Core.WriteDatabase
{
    public interface IWriteOnlyDatabase
    {
        void Insert<T>(T entity);
        TE Insert<T, TE>(T entity);
        void Update<T>(T entity);
        void Delete<T>(T entity);
    }
}
