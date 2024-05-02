using System;
using System.Linq.Expressions;
using System.Text;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Core.QueryGenerator;

namespace Thomas.Database.Core.Provider.Formatter
{
    internal readonly struct SqliteFormatter : ISqlFormatter
    {
        readonly SqlProvider ISqlFormatter.Provider => SqlProvider.Sqlite;
        readonly string ISqlFormatter.BindVariable => "$";
        readonly string ISqlFormatter.MinDate => "date('1900-01-01')";
        readonly string ISqlFormatter.MaxDate => "date('9999-12-31')";
        readonly string ISqlFormatter.CurrentDate => "date('now')";

        readonly string ISqlFormatter.Concatenate(params string[] values) => string.Join("||", values);

        readonly string ISqlFormatter.FormatOperator(string left, string right, ExpressionType expression) => expression switch
        {
            ExpressionType.AndAlso => $"({left} AND {right})",
            ExpressionType.And => $"({left} AND {right})",
            ExpressionType.OrElse => $"({left} OR {right})",
            ExpressionType.Or => $"({left} OR {right})",
            ExpressionType.Equal => $"({left} = {right})",
            ExpressionType.NotEqual => $"({left} <> {right})",
            ExpressionType.GreaterThan => $"({left} > {right})",
            ExpressionType.LessThan => $"({left} < {right})",
            ExpressionType.GreaterThanOrEqual => $"({left} >= {right})",
            ExpressionType.LessThanOrEqual => $"({left} <= {right})",
            ExpressionType.Divide => $"({left} / {right})",
            ExpressionType.Multiply => $"({left} * {right})",
            ExpressionType.Subtract => $"({left} - {right})",
            ExpressionType.Add => $"({left} + {right})",
            ExpressionType.Modulo => $"({left} % {right})",
            ExpressionType.Coalesce => $"IFNULL({left}, {right})",
            ExpressionType.Power => $"POWER({left}, {right})",
            _ => throw new NotImplementedException()
        };

        readonly string ISqlFormatter.GenerateInsert(string tableName, string[] columns, string[] values, DbColumn column, IParameterHandler parameterHandler, bool returnGenerateId = false)
        {
            var sb = new StringBuilder($"INSERT INTO {tableName}(")
                                    .AppendJoin(',', columns)
                                    .Append(") VALUES (")
                                    .AppendJoin(',', values)
                                    .Append(')');
            if (returnGenerateId)
            {
                return sb.Append($" RETURNING {column.DbName ?? column.Name}").ToString();
            }

            return sb.ToString();
        }

        readonly string ISqlFormatter.GenerateUpdate(string tableName, string tableAlias, string where, string[] columns)
        {
            /* 
               UPDATE Data AS A 
               SET UserName = :UserName
               WHERE (A.Id = :Id)
            */
            var stringBuilder = new StringBuilder($"UPDATE {tableName} AS {tableAlias} SET ")
                                   .AppendJoin(',', columns);

            if (!string.IsNullOrEmpty(where))
            {
                stringBuilder.Append($" WHERE {where}");
            }

            return stringBuilder.ToString();
        }

        readonly string ISqlFormatter.GenerateDelete(string tableName, string tableAlias, string where)
        {
            /* sample text:
                  DELETE FROM Data AS A WHERE A.Id = 1
            */
            if (where == null)
            {
                return $"DELETE FROM {tableName}";
            }

            return $"DELETE FROM {tableName} AS {tableAlias} WHERE {where}";
        }
    }
}
