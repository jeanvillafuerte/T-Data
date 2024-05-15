using System;
using System.Linq.Expressions;

namespace Thomas.Database.Core.WriteDatabase
{
    public interface IWriteOnlyDatabase
    {
        void Add<T>(T entity) where T : class, new();
        TE Add<T, TE>(T entity) where T : class, new();
        void Update<T>(T entity) where T : class, new();
        void Delete<T>(T entity) where T : class, new();
    }
}
