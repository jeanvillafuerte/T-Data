using System.Linq.Expressions;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Core.QueryGenerator;

namespace Thomas.Database.Core.Provider
{
    internal class SqlServerFormatter : ISqlFormatter
    {
        public SqlProvider Provider => SqlProvider.SqlServer;

        string ISqlFormatter.BindVariable => "@";
        string ISqlFormatter.MinDate => "CONVERT(DATETIME, N'1900-01-01', 102)";
        string ISqlFormatter.MaxDate => "CONVERT(DATETIME, N'9999-12-31', 102)";
        string ISqlFormatter.CurrentDate => "GETDATE()";

        public string CuratedColumnName(string name, string original = null)
        {
            return original ?? $"[{name}]";
        }

        public string CuratedTableName(string name, string original = null)
        {
            return original ?? $"[{name}]";
        }

        public string GenerateInsertSql(string tableName, string columns, string values, DbColumn column, IParameterHandler parameterHandler, bool returnGenerateId = false)
        {
            var baseInsert = $"INSERT INTO {tableName}({columns}) VALUES ({values})";
            return returnGenerateId ? $"{baseInsert} OUTPUT INSERTED.{column.DbName ?? column.Name}" : baseInsert;
        }

        public string FormatOperator(string left, string right, ExpressionType expression) => expression switch
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

        public string Concatenate(params string[] values)
        {
            return $"CONCAT({string.Join(",", values)})";
        }
    }

}
