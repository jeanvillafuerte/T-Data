using System;
using System.Linq.Expressions;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Core.QueryGenerator;

namespace Thomas.Database.Core.Provider
{
    internal class SqliteFormatter : ISqlFormatter
    {
        public SqlProvider Provider => throw new NotImplementedException();

        public string BindVariable => throw new NotImplementedException();

        public string MinDate => throw new NotImplementedException();

        public string MaxDate => throw new NotImplementedException();

        public string CurrentDate => throw new NotImplementedException();

        public string Concatenate(params string[] values)
        {
            throw new NotImplementedException();
        }

        public string CuratedColumnName(string name, string original = null)
        {
            throw new NotImplementedException();
        }

        public string CuratedTableName(string name, string original = null)
        {
            throw new NotImplementedException();
        }

        public string FormatOperator(string left, string right, ExpressionType expression)
        {
            throw new NotImplementedException();
        }

        public string GenerateInsertSql(string tableName, string columns, string values, DbColumn column, IParameterHandler parameterHandler, bool returnGenerateId = false)
        {
            throw new NotImplementedException();
        }
    }
}
