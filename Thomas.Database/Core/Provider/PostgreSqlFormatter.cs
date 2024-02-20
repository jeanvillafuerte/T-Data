using System;
using System.Linq.Expressions;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Core.QueryGenerator;

namespace Thomas.Database.Core.Provider
{
    internal class PostgreSqlFormatter : ISqlFormatter
    {
        public SqlProvider Provider => throw new NotImplementedException();

        public string BindVariable => throw new NotImplementedException();

        public string MinDate => throw new NotImplementedException();

        public string MaxDate => throw new NotImplementedException();

        public string CurrentDate => throw new NotImplementedException();

        public string Concatenate(params string[] values)
        {
            return $"CONCAT({string.Join(",", values)})";
        }

        public string CuratedColumnName(string name, string original = null)
        {
            return original ?? name;
        }

        public string CuratedTableName(string name, string original = null)
        {
            return original ?? name;
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
            ExpressionType.Coalesce => $"COALESCE({left}, {right})",
            ExpressionType.Power => $"POWER({left}, {right})",
            _ => throw new System.NotImplementedException()
        };

        public string GenerateInsertSql(string tableName, string columns, string values, DbColumn column, IParameterHandler parameterHandler, bool returnGenerateId = false)
        {
            var baseInsert = $"INSERT INTO {tableName}({columns}) VALUES ({values})";
            return returnGenerateId ? $"{baseInsert} RETURNING {column.DbName ?? column.Name}" : baseInsert;
        }
    }
}
