using System.Linq.Expressions;
using Thomas.Database.Core.FluentApi;

namespace Thomas.Database.Core.QueryGenerator
{
    internal interface ISqlFormatter
    {
        SqlProvider Provider { get; }
        string BindVariable { get; }
        string MinDate { get; }
        string MaxDate { get; }
        string CurrentDate { get; }
        string Concatenate(params string[] values);
        string GenerateInsert(string tableName, string[] columns, string[] values, DbColumn column, IParameterHandler parameterHandler, bool returnGenerateId = false);
        string GenerateUpdate(string tableName, string[] columns, string keyDbName, string propertyKeyName);
        string GenerateDelete(string tableName, string keyDbName, string propertyKeyName);
        string FormatOperator(string left, string right, ExpressionType expression);
    }

}
