using System.Linq.Expressions;
using System.Text;
using TData.Core.FluentApi;
using TData.Core.QueryGenerator;

namespace TData.Core.Provider.Formatter
{
    internal readonly struct SqlServerFormatter : ISqlFormatter
    {
        readonly SqlProvider ISqlFormatter.Provider => SqlProvider.SqlServer;
        readonly string ISqlFormatter.BindVariable => "@";
        readonly string ISqlFormatter.MinDate => "CONVERT(DATETIME, N'1900-01-01', 102)";
        readonly string ISqlFormatter.MaxDate => "CONVERT(DATETIME, N'9999-12-31', 102)";
        readonly string ISqlFormatter.CurrentDate => "GETDATE()";

        readonly string ISqlFormatter.PagingQuery(in string query)
        {
            return $"{query} OFFSET @{DatabaseHelperProvider.OFFSET_PARAMETER} ROWS FETCH NEXT @{DatabaseHelperProvider.PAGESIZE_PARAMETER} ROWS ONLY";
        }

        readonly string ISqlFormatter.Concatenate(params string[] values) => $"CONCAT({string.Join(",", values)})";

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
            ExpressionType.Coalesce => $"ISNULL({left}, {right})",
            ExpressionType.Power => $"POWER({left}, {right})",
            _ => throw new System.NotImplementedException()
        };

        readonly string ISqlFormatter.GenerateInsert(string tableName, string[] columns, string[] values, DbColumn column, bool returnGenerateId)
        {
            var sb = new StringBuilder($"INSERT INTO {tableName}(")
                                   .AppendJoin(',', columns)
                                   .Append(')');

            if (returnGenerateId)
            {
                sb.Append($" OUTPUT INSERTED.{column.DbName ?? column.Name}");
            }

            return sb.Append(" VALUES (")
                     .AppendJoin(',', values)
                     .Append(')')
                     .ToString();
        }

        readonly string ISqlFormatter.GenerateDelete(string tableName, string alias)
        {
            return $"DELETE {alias} FROM {tableName} {alias}";
        }

    }

}
