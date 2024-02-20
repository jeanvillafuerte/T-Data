using System.Linq.Expressions;
using System.Text;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Core.QueryGenerator;

namespace Thomas.Database.Core.Provider
{
    internal class OracleFormatter : ISqlFormatter
    {
        public SqlProvider Provider => SqlProvider.Oracle;

        string ISqlFormatter.BindVariable => ":";
        string ISqlFormatter.MinDate => "TO_DATE('01/01/1900', 'MM/DD/RRRR')";
        string ISqlFormatter.MaxDate => "TO_DATE('12/31/9999','MM/DD/YYYY')";
        string ISqlFormatter.CurrentDate => "SYSDATE";

        public string CuratedColumnName(string name, string original = null)
        {
            return original ?? $"{name.ToUpper()}";
        }

        public string CuratedTableName(string name, string original = null)
        {
            return original ?? name.ToUpper();
        }

        public string GenerateInsertSql(string tableName, string columns, string values, DbColumn keyColumn, IParameterHandler parameterHandler, bool returnGenerateId = false)
        {
            var stringBuilder = new StringBuilder();
            var baseInsert = $"INSERT INTO {tableName}({columns}) VALUES ({values})";

            if (!returnGenerateId)
                return baseInsert;

            parameterHandler.AddOutParam(keyColumn, out var paramName);

            stringBuilder.Append("BEGIN ");
            stringBuilder.Append($"{baseInsert} RETURNING {CuratedColumnName(keyColumn.Name, keyColumn.DbName)} INTO {paramName};");
            stringBuilder.Append("END;");

            return stringBuilder.ToString();
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
            ExpressionType.Modulo => $"MOD({left}, {right})",
            ExpressionType.Coalesce => $"NVL({left}, {right})",
            ExpressionType.Power => $"POWER({left}, {right})",
            _ => throw new System.NotImplementedException()
        };

        public string Concatenate(params string[] values)
        {
            return string.Join(" || ", values);
        }
    }
}
