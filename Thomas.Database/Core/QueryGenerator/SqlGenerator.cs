using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Thomas.Database.Cache;
using Thomas.Database.Configuration;
using Thomas.Database.Core.Converters;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Helpers;

namespace Thomas.Database.Core.QueryGenerator
{
    public interface IParameterHandler
    {
        void AddInParam(in Type propertyType, object value, out string paramName);
        void AddOutParam(DbColumn column, out string paramName);
    }

    internal class SqlGenerator<T> : IParameterHandler where T : class, new()
    {

        private string TableAlias;
        private readonly Type Type;
        private string DbSchema
        {
            get
            {
                if (_table.Schema != null)
                    return $"{_table.Schema}.";
                else
                    return string.Empty;
            }
        }
        private string TableName
        {
            get
            {
                return $"{DbSchema}{_formatter.CuratedTableName(_table.Name, _table.DbName)} {TableAlias}";
            }
        }
        private string TableNameWithoutAlias
        {
            get
            {
                return $"{DbSchema}{_formatter.CuratedTableName(_table.Name, _table.DbName)}";
            }
        }

        private readonly string _columns;
        private const string SELECT = "SELECT ";
        private const string FROM = "FROM";
        private const string WHERE = " WHERE ";
        private const string UPDATE = "UPDATE";
        private const string DELETE = "DELETE";
        private readonly DbTable _table;
        private readonly ITypeConversionStrategy[] _converters;
        private readonly ISqlFormatter _formatter;
        private readonly CultureInfo _culture;

        public Dictionary<string, QueryParameter> DbParametersToBind { get; set; }

        //TODO: support for multiple tables
        //support convert without table configuration (OK)
        //cache de SQL script and just obtain the parameter values to save in DbParamteresToBind (OK)
        public SqlGenerator(ISqlFormatter formatter, in CultureInfo culture, in ITypeConversionStrategy[] converters)
        {
            _converters = converters;
            _formatter = formatter;
            _culture = culture;
            Type = typeof(T);
            TableAlias = GetTableAlias();

            if (!DbConfigurationFactory.Tables.ContainsKey(Type.FullName!))
            {
                var builder = new TableBuilder();
                DbTable dbTable = builder.Configure<T>();
                dbTable.AddFieldsAsColumns<T>();
                DbFactory.AddDbBuilder(builder);
            }

            _table = DbConfigurationFactory.Tables[Type.FullName!];
            _columns = string.Join(',', _table.Columns.Select(x => x.FullDbName).ToArray());
            DbParametersToBind = new Dictionary<string, QueryParameter>();
        }

        public string[][] GetAlias(LambdaExpression predicate)
        {
            string[][] aliasIdentifiers = new string[1][];
            var tableIdentifier = predicate!.Parameters[0]!.Name;
            aliasIdentifiers[0] = new string[] { tableIdentifier!, TableAlias };
            return aliasIdentifiers;
        }

        public string GenerateSelectWhere(Expression<Func<T, bool>>? predicate)
        {
            var key = ExpressionHasher.GetHashCode(predicate, _formatter.Provider, false).ToString();

            if (GetSQL(key, predicate, out var sqlText))
                return sqlText;
            else
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append(SELECT);
                stringBuilder.Append(_columns);
                stringBuilder.Append($" {FROM} {TableName}");

                if (predicate != null)
                {
                    stringBuilder.Append(WHERE);
                    stringBuilder.Append(WhereClause(predicate!.Body, GetAlias(predicate)));
                }

                sqlText = stringBuilder.ToString();

                CacheQueryString.Set(key, sqlText);
            }

            return sqlText;
        }

        private bool GetSQL(string key, Expression<Func<T, bool>>? predicate, out string value)
        {
            if (CacheQueryString.TryGet(key, out value))
            {
                var valueExtractor = new ExpressionValueExtractor<T>(this);
                valueExtractor.LoadParameterValues(predicate);
                return true;
            }

            return false;
        }

        private string WhereClause(Expression expression, string[][] aliasIdentifiers = null)
        {
            return expression switch
            {
                ConstantExpression constantExpression => HandleConstantExpression(constantExpression, aliasIdentifiers),
                UnaryExpression unaryExpression => HandleUnaryExpression(unaryExpression, aliasIdentifiers),
                BinaryExpression binaryExpression => HandleBinaryExpression(binaryExpression, aliasIdentifiers),
                MemberExpression memberExpression when memberExpression.Member.Name == "MinValue" && memberExpression.Type == typeof(DateTime) => SqlMin(),
                MemberExpression memberExpression when memberExpression.Member.Name == "MaxValue" && memberExpression.Type == typeof(DateTime) => SqlMax(),
                MemberExpression memberExpression when memberExpression.Type == typeof(string) => GetColumnName(memberExpression, aliasIdentifiers),
                MemberExpression memberExpression when memberExpression.Type == typeof(int) ||
                                                       memberExpression.Type == typeof(short) ||
                                                       memberExpression.Type == typeof(long) ||
                                                       memberExpression.Type == typeof(decimal) ||
                                                       memberExpression.Type == typeof(DateTime) ||
                                                       memberExpression.Type == typeof(float) ||
                                                       memberExpression.Type.IsArray ||
                                                       (memberExpression.Type.IsGenericType &&
                                                       typeof(IEnumerable).IsAssignableFrom(memberExpression.Type)) => HandleMemberExpression(memberExpression, aliasIdentifiers),
                NewExpression newExpression => HandleNewExpression(newExpression),
                NewArrayExpression newArrayExpression => HandleNewArrayExpression(newArrayExpression),
                MemberExpression memberExpression when memberExpression.Type == typeof(bool) => $"{GetColumnName(memberExpression, aliasIdentifiers)} = 1",
                MethodCallExpression methodCall when SqlGenerator<T>.IsStringContains(methodCall) => HandleStringContains(methodCall, StringOperator.Contains, aliasIdentifiers),
                MethodCallExpression methodCall when SqlGenerator<T>.IsEnumerableContains(methodCall) => HandleEnumerableContains(methodCall, aliasIdentifiers),
                MethodCallExpression methodCall when SqlGenerator<T>.IsEquals(methodCall) => HandleStringContains(methodCall, StringOperator.Equals, aliasIdentifiers),
                MethodCallExpression methodCall when SqlGenerator<T>.IsStartsWith(methodCall) => HandleStringContains(methodCall, StringOperator.StartsWith, aliasIdentifiers),
                MethodCallExpression methodCall when SqlGenerator<T>.IsEndsWith(methodCall) => HandleStringContains(methodCall, StringOperator.EndsWith, aliasIdentifiers),
                MethodCallExpression methodCall when SqlGenerator<T>.IsBetween(methodCall) => HandleBetween(methodCall, aliasIdentifiers),
                MethodCallExpression methodCall when SqlGenerator<T>.IsExists(methodCall) => HandleExists(methodCall),
                LambdaExpression lambdaExpression => WhereClause(lambdaExpression.Body, aliasIdentifiers),
                MethodCallExpression methodCall => throw new NotSupportedException(),
                ListInitExpression listInitExpression => throw new NotSupportedException(),
                DynamicExpression dynamicExpression => throw new NotSupportedException(),
                ConditionalExpression conditionalExpression => throw new NotSupportedException(),
                GotoExpression gotoExpression => throw new NotSupportedException(),
                IndexExpression indexExpression => throw new NotSupportedException(),
                InvocationExpression invocationExpression => throw new NotSupportedException(),
                LabelExpression labelExpression => throw new NotSupportedException(),
                LoopExpression loopExpression => throw new NotSupportedException(),
                MemberInitExpression memberInitExpression => throw new NotSupportedException(),
                SwitchExpression switchExpression => throw new NotSupportedException(),
                TryExpression tryExpression => throw new NotSupportedException(),
                TypeBinaryExpression typeBinaryExpression => throw new NotSupportedException(),
                _ => throw new NotImplementedException(),
            };
        }

        #region parameter handlers
        public void AddInParam(in Type propertyType, object value, out string paramName)
        {
            var counter = DbParametersToBind.Count + 1;
            paramName = $"{_formatter.BindVariable}param{counter}";

            var convertedValue = TypeConversionRegistry.ConvertByType(value, propertyType, _culture, _converters);
            DbParametersToBind.Add(paramName, new QueryParameter(convertedValue, false, propertyType));
        }

        public void AddOutParam(DbColumn column, out string paramName)
        {
            var counter = DbParametersToBind.Count + 1;
            paramName = $"{_formatter.BindVariable}param{counter}";
            DbParametersToBind.Add(paramName, new QueryParameter(null, true, column.Property.PropertyType));
        }
        #endregion

        #region Write Operations

        public string GenerateUpdate<T>(in T value, Expression<Func<T, object>> predicate)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"{UPDATE} {TableName} SET ");

            var columns = _table.Columns.Where(x => x.Name != _table.Key.Name).ToList();
            for (int i = 0; i < columns.Count; i++)
            {
                this.AddInParam(columns[i].Property.PropertyType, GetValue(columns[i].Property, value), out var paramName);
                stringBuilder.Append($"{_formatter.CuratedColumnName(columns[i].Name, columns[i].DbName)} = {paramName}");
                if (columns.Count != i + 1)
                    stringBuilder.Append(",");
            }

            stringBuilder.Append(WHERE);
            stringBuilder.Append(WhereClause(predicate!.Body, GetAlias(predicate)));

            return stringBuilder.ToString();
        }

        public string GenerateInsert<T>(in T value, bool returnGenerateId = false)
        {
            foreach (var column in _table.Columns.Where(x => !x.Autogenerated))
                AddInParam(column.Property.PropertyType, GetValue(column.Property, value), out var _);

            var values = string.Join(",", DbParametersToBind.Select(x => x.Key).ToArray());
            var columns = string.Join(',', _table.Columns.Where(x => !x.Autogenerated).Select(x => x.DbName).ToArray());

            return _formatter.GenerateInsertSql(TableNameWithoutAlias, columns, values, _table.Key, this, returnGenerateId);
        }

        public string GenerateDelete<T>(Expression<Func<T, object>> predicate)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"{DELETE} {FROM} {TableName}");
            stringBuilder.Append(WHERE);
            stringBuilder.Append(WhereClause(predicate!.Body, GetAlias(predicate)));
            return stringBuilder.ToString();
        }

        private static object GetValue<T>(PropertyInfo prop, T value)
        {
            var rawValue = prop.GetValue(value);

            if (rawValue is null ||
                default(DateTime).Equals(rawValue) ||
                default(Guid).Equals(rawValue))
                return DBNull.Value;
            else
                return rawValue;
        }

        #endregion

        #region Handlers

        private string HandleExists(MethodCallExpression methodCall)
        {
            var lambdaExpression = methodCall.Arguments[0] as LambdaExpression;
            var internalTableAlias = GetTableAlias();
            var a = lambdaExpression.Parameters[0];
            var b = lambdaExpression.Parameters[1];

            string where = "";
            string tableName = b.Type.Name;

            string[][] aliasIdentifier = new string[2][];

            if (a.Type == typeof(T))
            {
                aliasIdentifier[0] = new string[] { a.Name, TableAlias, };
                aliasIdentifier[1] = new string[] { b.Name, internalTableAlias, };
            }
            else
            {
                aliasIdentifier[0] = new string[] { a.Name, internalTableAlias };
                aliasIdentifier[1] = new string[] { b.Name, TableAlias };
            }


            if (lambdaExpression.Body != null)
            {
                where = $" WHERE {WhereClause(lambdaExpression.Body, aliasIdentifier)}";
            }

            return $"EXISTS (SELECT 1 FROM {tableName} {internalTableAlias}{where})";
        }

        private string HandleNewArrayExpression(NewArrayExpression newArrayExpression)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < newArrayExpression.Expressions.Count; i++)
            {
                builder.Append(WhereClause(newArrayExpression.Expressions[i]));
                if (i < newArrayExpression.Expressions.Count - 1)
                    builder.Append(",");
            }

            return builder.ToString();
        }

        private string HandleBetween(MethodCallExpression methodCall, string[][] aliasIdentifier = null)
        {
            var expression = (methodCall.Arguments[0] as LambdaExpression).Body as UnaryExpression;

            var minValue = WhereClause(methodCall.Arguments[1]);
            var maxValue = WhereClause(methodCall.Arguments[2]);

            return $"{WhereClause(expression.Operand, aliasIdentifier)} BETWEEN {minValue} AND {maxValue}";
        }

        private string SqlMin() => _formatter.MinDate;
        private string SqlMax() => _formatter.MaxDate;

        private string HandleMemberExpression(MemberExpression memberExpression, string[][] aliasIdentifier = null)
        {
            if (memberExpression.Expression is ConstantExpression ce && memberExpression.Member is FieldInfo fe)
            {
                var constant = Expression.Constant(fe.GetValue(ce.Value));
                if (constant.Type.IsArray || (constant.Type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(constant.Type)))
                {
                    var @params = new HashSet<string>();

                    if (typeof(IEnumerable<int>).IsAssignableFrom(constant.Type))
                        AddArrayConstantValues<int>(typeof(int), constant, @params);
                    else if (typeof(IEnumerable<short>).IsAssignableFrom(constant.Type))
                        AddArrayConstantValues<short>(typeof(short), constant, @params);
                    else if (typeof(IEnumerable<long>).IsAssignableFrom(constant.Type))
                        AddArrayConstantValues<long>(typeof(long), constant, @params);
                    else if (typeof(IEnumerable<decimal>).IsAssignableFrom(constant.Type))
                        AddArrayConstantValues<decimal>(typeof(decimal), constant, @params);
                    else if (typeof(IEnumerable<float>).IsAssignableFrom(constant.Type))
                        AddArrayConstantValues<float>(typeof(float), constant, @params);
                    else if (typeof(IEnumerable<DateTime>).IsAssignableFrom(constant.Type))
                        AddArrayConstantValues<DateTime>(typeof(DateTime), constant, @params);

                    return $"{string.Join(",", @params.ToArray())}";
                }
                else
                {
                    AddInParam(constant.Type, constant.Value, out var paramName);
                    return paramName;
                }
            }
            else
            {
                return GetColumnName(memberExpression, aliasIdentifier);
            }
        }

        private IEnumerable<T> GetValues<T>(ConstantExpression expression)
        {
            var instantiator = Expression.Lambda<Func<IEnumerable<T>>>(expression).Compile();
            return instantiator();
        }

        private void AddArrayConstantValues<T>(Type type, ConstantExpression expression, HashSet<string> @params)
        {
            foreach (var value in GetValues<T>(expression))
            {
                AddInParam(type, value, out var paramName);
                @params.Add(paramName);
            }
        }

        private string HandleNewExpression(NewExpression newExpression)
        {
            if (newExpression.Type == typeof(DateTime))
            {
                var instantiator = Expression.Lambda<Func<DateTime>>(newExpression).Compile();
                var value = instantiator();

                AddInParam(typeof(DateTime), value, out var paramName);

                return paramName;
            }

            return "";
        }

        private string HandleConstantExpression(ConstantExpression constantExpression, string[][] aliasIdentifiers = null)
        {
            if (constantExpression.Value == null)
                return "IS NULL";
            else
            {
                AddInParam(constantExpression.Type, constantExpression.Value, out var paramName);
                return paramName;
            }
        }

        private string HandleUnaryExpression(UnaryExpression expression, string[][] aliasIdentifiers = null)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Not:
                    var operand = WhereClause(expression.Operand, aliasIdentifiers);
                    return $"NOT ({operand})";
                case ExpressionType.Convert:
                    return WhereClause(expression.Operand, aliasIdentifiers);
                default:
                    throw new NotSupportedException("Unsupported unary operator");
            }
        }

        private string HandleBinaryExpression(BinaryExpression expression, string[][] aliasIdentifiers = null)
        {
            string left;
            string right;
            if (expression.Left is MemberExpression && expression.Right is MemberExpression)
            {
                left = WhereClause(expression.Left, aliasIdentifiers);
                right = WhereClause(expression.Right, aliasIdentifiers);
            }
            else
            {
                left = WhereClause(expression.Left, aliasIdentifiers);

                if (expression.Right is ConstantExpression constantExpression && constantExpression.Value == null)
                {
                    if (expression.NodeType == ExpressionType.Equal)
                        return $"{left} IS NULL";
                    else if (expression.NodeType == ExpressionType.NotEqual)
                        return $"{left} IS NOT NULL";
                }

                right = WhereClause(expression.Right, aliasIdentifiers);

                if (expression.Left is ConstantExpression constantExpression2 && constantExpression2.Value == null)
                {
                    if (expression.NodeType == ExpressionType.Equal)
                        return $"{right} IS NULL";
                    else if (expression.NodeType == ExpressionType.NotEqual)
                        return $"{right} IS NOT NULL";
                }
            }

            return _formatter.FormatOperator(left, right, expression.NodeType);
        }

        private string HandleStringContains(MethodCallExpression expression, StringOperator @operator, string[][] aliasIdentifiers = null)
        {
            if (expression.Object is MemberExpression memberExpression
                && expression.Arguments[0] is ConstantExpression constantExpression && @operator != StringOperator.Equals)
            {
                var column = GetColumnName(memberExpression, aliasIdentifiers);
                var value = constantExpression.Value.ToString();
                var initialOperator = @operator == StringOperator.StartsWith ? "" : "%";
                var finalOperator = @operator == StringOperator.EndsWith ? "" : "%";
                AddInParam(typeof(string), value, out var varName);
                var likeSQL = _formatter.Concatenate($"'{initialOperator}'", varName, $"'{finalOperator}'");
                return $"{column} LIKE {likeSQL}";  // Caution: Be aware of SQL injection risks here
            }
            else if (expression.Object is MemberExpression memberExpression2
                && expression.Arguments[0] is ConstantExpression constantExpression2
                && @operator == StringOperator.Equals)
            {
                var column = GetColumnName(memberExpression2, aliasIdentifiers);
                var value = WhereClause(constantExpression2, aliasIdentifiers);
                return $"{column} = {value}";
            }

            throw new NotSupportedException("Unsupported method call");
        }

        private string HandleEnumerableContains(MethodCallExpression expression, string[][] aliasIdentifiers = null)
        {
            if (expression.Arguments.Count == 2)
            {
                var values = WhereClause(expression.Arguments[0], aliasIdentifiers);
                var column = WhereClause(expression.Arguments[1], aliasIdentifiers);
                return $"{column} IN ({values})";
            }
            else if (expression.Arguments.Count == 1)
            {
                var values = WhereClause(expression.Object, aliasIdentifiers);
                var column = WhereClause(expression.Arguments[0], aliasIdentifiers);
                return $"{column} IN ({values})";
            }

            return "";
        }

        internal static bool IsBetween(MethodCallExpression expression) =>
                     expression.Method.DeclaringType.Name == "SqlExpression"
                           && expression.Method.Name == "Between";

        internal static bool IsExists(MethodCallExpression expression) =>
                     expression.Method.DeclaringType.Name == "SqlExpression"
                           && expression.Method.Name == "Exists";

        internal static bool IsEnumerableContains(MethodCallExpression expression) =>
                     (typeof(IEnumerable).IsAssignableFrom(expression.Method.DeclaringType) ||
                      typeof(System.Linq.Enumerable).IsAssignableFrom(expression.Method.DeclaringType))
                           && expression.Method.Name == "Contains";

        internal static bool IsStringContains(MethodCallExpression expression) =>
                     expression.Method.DeclaringType == typeof(string)
                           && expression.Method.Name == "Contains";

        internal static bool IsEquals(MethodCallExpression expression) =>
                     expression.Method.DeclaringType == typeof(string)
                           && expression.Method.Name == "Equals";

        internal static bool IsStartsWith(MethodCallExpression expression) =>
                     expression.Method.DeclaringType == typeof(string)
                           && expression.Method.Name == "StartsWith";

        internal static bool IsEndsWith(MethodCallExpression expression) =>
                     expression.Method.DeclaringType == typeof(string)
                           && expression.Method.Name == "EndsWith";

        private int indexTableAlias = 0;
        private string GetTableAlias()
        {
            return "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring(indexTableAlias++, 1);
        }

        private string GetColumnName(MemberExpression member, string[][] aliasIdentifiers = null)
        {
            var dbColumn = _table.Columns.First(x => x.Name == member.Member.Name);

            if (member.Member.ReflectedType == typeof(T))
                return $"{TableAlias}.{_formatter.CuratedColumnName(dbColumn.Name, dbColumn.DbName)}";

            var tableTempAlias = member.Expression!.ToString().Split('.')[0];
            return $"{ReplaceIdentifier(tableTempAlias, aliasIdentifiers)}.{_formatter.CuratedColumnName(dbColumn.Name, dbColumn.DbName)}";
        }
        private static string ReplaceIdentifier(string expression, string[][] aliasIdentifiers)
        {
            for (int i = 0; i < aliasIdentifiers.Length; i++)
                if (aliasIdentifiers[i][0] == expression)
                    return aliasIdentifiers[i][1];

            return expression;
        }
        #endregion
    }

    internal enum StringOperator
    {
        Contains,
        Equals,
        StartsWith,
        EndsWith
    }
}
