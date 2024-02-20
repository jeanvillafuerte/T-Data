using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Thomas.Database.Core
{
    public interface IDbSetExpression
    {
        T? ToSingle<T>(Expression<Func<T, bool>>? where = null) where T : class, new();
        IEnumerable<T> ToList<T>(Expression<Func<T, bool>>? where = null) where T : class, new();
        Task<IEnumerable<T>> ToListAsync<T>(Expression<Func<T, bool>>? where) where T : class, new();
        Task<IEnumerable<T>> ToListAsync<T>(Expression<Func<T, bool>>? where, [EnumeratorCancellation] CancellationToken cancellationToken) where T : class, new();
    }
}
