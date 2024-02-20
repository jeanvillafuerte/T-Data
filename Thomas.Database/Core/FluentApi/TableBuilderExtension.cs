using System;
using System.Linq;
using System.Linq.Expressions;

namespace Thomas.Database.Core.FluentApi
{
    public static class TableBuilderExtension
    {
        public static DbTable Schema(this DbTable table, string schema)
        {
            table.Schema = schema;
            return table;
        }

        public static DbTable AddFieldsAsColumns<T>(this DbTable table)
        {
            TableBuilder.AddColumns<T>(table, table.Key?.Name, typeof(T));
            return table;
        }

        public static DbTable DbName(this DbTable table, string dbName)
        {
            table.DbName = dbName;
            return table;
        }

        public static DbTable ExcludeColumn<T>(this DbTable table, Expression<Func<T, object>> expression)
        {
            var memberExpression = TableBuilder.EnsureSelectedMember(expression);
            var columnName = memberExpression.Member.Name;
            var index = table.Columns.FindIndex(x => x.Name == columnName);
            table.Columns.RemoveAt(index);
            return table;
        }

        public static DbColumn SelectColumn<T>(this DbTable table, Expression<Func<T, object>> expression)
        {
            var memberExpression = TableBuilder.EnsureSelectedMember(expression);
            return table.Columns.First(x => x.Name == memberExpression.Member.Name);
        }

        public static DbColumn DbName<T>(this DbColumn dbColumn, string dbName)
        {
            dbColumn.DbName = dbName;
            return dbColumn;
        }
    }
}
