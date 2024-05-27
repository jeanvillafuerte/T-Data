using System.Linq.Expressions;
using System.Text;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Core.QueryGenerator;

namespace Thomas.Database.Core.Provider.Formatter
{
    internal readonly struct OracleFormatter : ISqlFormatter
    {
        readonly SqlProvider ISqlFormatter.Provider => SqlProvider.Oracle;

        readonly string ISqlFormatter.BindVariable => ":";
        readonly string ISqlFormatter.MinDate => "TO_DATE('01/01/1900', 'MM/DD/RRRR')";
        readonly string ISqlFormatter.MaxDate => "TO_DATE('12/31/9999','MM/DD/YYYY')";
        readonly string ISqlFormatter.CurrentDate => "SYSDATE";

        readonly string ISqlFormatter.Concatenate(params string[] values) => string.Join(" || ", values);

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
            ExpressionType.Coalesce => $"NVL({left}, {right})",
            ExpressionType.Power => $"POWER({left}, {right})",
            _ => throw new System.NotImplementedException()
        };

        readonly string ISqlFormatter.GenerateInsert(string tableName, string[] columns, string[] values, DbColumn keyColumn, bool returnGenerateId = false)
        {
            if (returnGenerateId)
            {
                return new StringBuilder("BEGIN ")
                                        .Append($"INSERT INTO {tableName}(")
                                        .AppendJoin(',', columns)
                                        .Append(") VALUES (")
                                        .AppendJoin(',', values)
                                        .Append(')')
                                        .Append(" RETURNING ")
                                        .Append(keyColumn.DbName ?? keyColumn.Name)
                                        .Append($" INTO :{keyColumn.Name}; END;")
                                        .ToString();
            }

            return new StringBuilder($"INSERT INTO {tableName}(")
                                        .AppendJoin(',', columns)
                                        .Append(") VALUES (")
                                        .AppendJoin(',', values)
                                        .Append(')').ToString();
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
            return $"DELETE {tableName} WHERE {keyDbName} = :{propertyKeyName}";
        }
    }
}
