using System;
using System.Linq.Expressions;
using System.Text;
using TData.Core.FluentApi;
using TData.Core.QueryGenerator;

namespace TData.Core.Provider.Formatter
{
    internal readonly struct MySqlFormatter : ISqlFormatter
    {
        readonly DbProvider ISqlFormatter.Provider => DbProvider.MySql;
        readonly string ISqlFormatter.BindVariable => "@";
        readonly string ISqlFormatter.MinDate => "STR_TO_DATE('01/01/1900', '%m/%d/%Y')";
        readonly string ISqlFormatter.MaxDate => "STR_TO_DATE('12/31/9999', '%m/%d/%Y')";
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
            _ => throw new NotImplementedException()
        };

        readonly string ISqlFormatter.GenerateInsert(string tableName, string[] columns, string[] values, DbColumn keyColumn, bool returnGenerateId)
        {
            var stringBuilder = new StringBuilder($"INSERT INTO {tableName}(")
                                .AppendJoin(',', columns)
                                .Append(") VALUES (")
                                .AppendJoin(',', values)
                                .Append(')');

            if (returnGenerateId)
            {
                return stringBuilder.Append("; SELECT LAST_INSERT_ID();").ToString();
            }

            return stringBuilder.ToString();
        }

        readonly string ISqlFormatter.GenerateDelete(string tableName, string alias)
        {
            return $"DELETE FROM {tableName} {alias}";
        }

        public string PagingQuery(in string query)
        {
            return $"{query} LIMIT @{DatabaseHelperProvider.OFFSET_PARAMETER}, @{DatabaseHelperProvider.PAGESIZE_PARAMETER}";
        }
    }
}
