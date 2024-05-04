using System.Linq.Expressions;
using System.Text;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Core.QueryGenerator;

namespace Thomas.Database.Core.Provider.Formatter
{
    internal readonly struct PostgreSqlFormatter : ISqlFormatter
    {
        readonly SqlProvider ISqlFormatter.Provider => SqlProvider.PostgreSql;
        readonly string ISqlFormatter.BindVariable => ":";
        readonly string ISqlFormatter.MinDate => "CAST('1900-01-01' AS date)";
        readonly string ISqlFormatter.MaxDate => "CAST('9999-12-31' AS date)";
        readonly string ISqlFormatter.CurrentDate => "CURRENT_DATE";

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

        readonly string ISqlFormatter.GenerateUpdate(string tableName, string[] columns, string keyDbName, string propertyKeyName)
        {
            return new StringBuilder($"UPDATE {tableName} SET ")
                                    .AppendJoin(',', columns)
                                    .Append($" WHERE {keyDbName} = :{propertyKeyName}")
                                    .ToString();
        }

        readonly string ISqlFormatter.GenerateDelete(string tableName, string keyDbName, string propertyKeyName)
        {
            /* sample text:
                    DELETE FROM Data A WHERE A.Id = 1
            */
            return $"DELETE FROM {tableName} WHERE {keyDbName} = :{propertyKeyName}";
        }
    }
}
