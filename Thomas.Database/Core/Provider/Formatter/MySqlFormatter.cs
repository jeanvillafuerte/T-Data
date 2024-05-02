using System;
using System.Linq.Expressions;
using System.Text;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Core.QueryGenerator;

namespace Thomas.Database.Core.Provider.Formatter
{
    internal readonly struct MySqlFormatter : ISqlFormatter
    {
        readonly SqlProvider ISqlFormatter.Provider => SqlProvider.MySql;
        readonly string ISqlFormatter.BindVariable => "@";
        readonly string ISqlFormatter.MinDate => "STR_TO_DATE('01/01/1900', '%m/%d/%Y')";
        readonly string ISqlFormatter.MaxDate => "STR_TO_DATE('12/31/9999', '%m/%d/%Y')";
        readonly string ISqlFormatter.CurrentDate => "CURDATE()";

        string ISqlFormatter.Concatenate(params string[] values)
        {
            return $"CONCAT({string.Join(",", values)})";
        }

        string ISqlFormatter.FormatOperator(string left, string right, ExpressionType expression) => expression switch
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

        string ISqlFormatter.GenerateInsert(string tableName, string[] columns, string[] values, DbColumn keyColumn, IParameterHandler parameterHandler, bool returnGenerateId = false)
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

        string ISqlFormatter.GenerateUpdate(string tableName, string tableAlias, string where, string[] columns)
        {
            var stringBuilder = new StringBuilder($"UPDATE {tableName} {tableAlias} SET ")
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
                    DELETE FROM Data A WHERE A.Id = 1
            */

            if (where == null)
            {
                return $"DELETE FROM {tableName}";
            }

            return $"DELETE FROM {tableName} {tableAlias} WHERE {where}";
        }
    }
}
