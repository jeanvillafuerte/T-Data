using System;
using System.Linq.Expressions;

namespace Thomas.Database.Core.WriteDatabase
{
    public interface IWriteOnlyDatabase
    {
        void Add<T>(T entity);
        TE Add<T, TE>(T entity);
        void Update<T>(T entity);
        void Delete<T>(T entity);
    }
}
