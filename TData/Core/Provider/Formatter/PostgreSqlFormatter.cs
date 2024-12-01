using System.Linq.Expressions;
using System.Text;
using TData.Core.FluentApi;
using TData.Core.QueryGenerator;

namespace TData.Core.Provider.Formatter
{
    internal readonly struct PostgreSqlFormatter : ISqlFormatter
    {
        readonly SqlProvider ISqlFormatter.Provider => SqlProvider.PostgreSql;
        readonly string ISqlFormatter.BindVariable => "@";
        readonly string ISqlFormatter.MinDate => "CAST('1900-01-01' AS date)";
        readonly string ISqlFormatter.MaxDate => "CAST('9999-12-31' AS date)";
        readonly string ISqlFormatter.CurrentDate => "NOW()";

        readonly string ISqlFormatter.Concatenate(params string[] values)
        {
            return $"CONCAT({string.Join(",", values)})";
        }

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
            ExpressionType.Modulo => $"MOD({left}, {right})",
            ExpressionType.Coalesce => $"COALESCE({left}, {right})",
            ExpressionType.Power => $"POWER({left}, {right})",
            _ => throw new System.NotImplementedException()
        };

        readonly string ISqlFormatter.GenerateInsert(string tableName, string[] columns, string[] values, DbColumn column, bool returnGenerateId)
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

        readonly string ISqlFormatter.GenerateDelete(string tableName, string alias)
        {
            return $"DELETE FROM {tableName} {alias}";
        }

        public string PagingQuery(in string query)
        {
            return $"{query} LIMIT @{DatabaseHelperProvider.PAGESIZE_PARAMETER} OFFSET @{DatabaseHelperProvider.OFFSET_PARAMETER}";
        }
    }
}
