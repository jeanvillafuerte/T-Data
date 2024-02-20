using System.Linq.Expressions;
using Thomas.Database.Core.FluentApi;

namespace Thomas.Database.Core.QueryGenerator
{
    public interface ISqlFormatter
    {
        SqlProvider Provider { get; }
        string BindVariable { get; }
        string MinDate { get; }
        string MaxDate { get; }
        string CurrentDate { get; }
        string Concatenate(params string[] values);
        string CuratedTableName(string name, string original = null);
        string CuratedColumnName(string name, string original = null);
        string GenerateInsertSql(string tableName, string columns, string values, DbColumn column, IParameterHandler parameterHandler, bool returnGenerateId = false);
        string FormatOperator(string left, string right, ExpressionType expression);
    }

}
