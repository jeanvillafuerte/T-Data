using System;
using System.Linq.Expressions;

namespace Thomas.Database.Core.WriteDatabase
{
    /// <summary>
    /// Interface for write-only database operations.
    /// </summary>
    public interface IWriteOnlyDatabase
    {
        /// <summary>
        /// Inserts a new entity into the database.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to insert.</param>
        void Insert<T>(T entity);

        /// <summary>
        /// Inserts a new entity into the database and returns a specified type.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <typeparam name="TE">The type to return.</typeparam>
        /// <param name="entity">The entity to insert.</param>
        /// <returns>An instance of type <typeparamref name="TE"/>.</returns>
        TE Insert<T, TE>(T entity);

        /// <summary>
        /// Updates an existing entity in the database.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to update.</param>
        void Update<T>(T entity);

        /// <summary>
        /// Deletes an existing entity from the database.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity to delete.</param>
        void Delete<T>(T entity);

        //TODO: complete implementation
        /// <summary>
        /// Truncates a table in the database.
        /// </summary>
        /// <typeparam name="T">The type representing the table.</typeparam>
        /// <param name="forceResetAutoIncrement">If set to <c>true</c>, resets the auto-increment value.</param>
        //void Truncate<T>(bool forceResetAutoIncrement = false);

        /// <summary>
        /// Truncates a table in the database by table name.
        /// </summary>
        /// <param name="tableName">The name of the table to truncate.</param>
        /// <param name="forceResetAutoIncrement">If set to <c>true</c>, resets the auto-increment value.</param>
        //void Truncate(string tableName, bool forceResetAutoIncrement = false);

        /// <summary>
        /// Updates entities in the database that match the specified condition.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="condition">The condition to match entities.</param>
        /// <param name="updates">The fields and values to update.</param>
        void UpdateIf<T>(Expression<Func<T, bool>> condition, params (Expression<Func<T, object>> field, object value)[] updates);

        /// <summary>
        /// Deletes entities in the database that match the specified condition.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="condition">The condition to match entities.</param>
        void DeleteIf<T>(Expression<Func<T, bool>> condition);
    }
}
