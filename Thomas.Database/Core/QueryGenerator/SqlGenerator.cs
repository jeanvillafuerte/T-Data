using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Thomas.Database.Cache;
using Thomas.Database.Configuration;
using Thomas.Database.Core.Converters;
using Thomas.Database.Core.FluentApi;
using Thomas.Database.Core.Provider;
using Thomas.Database.Helpers;

[assembly: InternalsVisibleTo("Thomas.Cache")]

namespace Thomas.Database.Core.QueryGenerator
{
    public interface IParameterHandler
    {
        void AddInParam(in Type propertyType, object value, string dbType, out string paramName);
        void AddInParam(in PropertyInfo propertyInfo);
        void AddInParam(in PropertyInfo propertyInfo, out string paramName);
        void AddOutParam(DbColumn column, out string paramName);
    }

    internal partial class SQLGenerator<T> : IParameterHandler
    {
        private readonly string TableAlias;
        private readonly Type Type;
        private string DbSchema
        {
            get
            {
                if (_table.Schema != null)
                    return $"{_table.Schema}.";
                return string.Empty;
            }
        }
        private string TableName
        {
            get
            {
                return $"{DbSchema}{_table.DbName ?? _table.Name} {TableAlias}";
            }
        }
        private string TableNameWithoutAlias
        {
            get
            {
                return DbSchema + (_table.DbName ?? _table.Name);
            }
        }

        private readonly DbTable _table;
        private readonly ISqlFormatter _formatter;

        /// <summary>
        /// considering is an arbitrary amount of parameters, a linked list is the best option
        /// </summary>
        internal readonly LinkedList<DbParameterInfo> DbParametersToBind;

        public SQLGenerator(in ISqlFormatter formatter)
        {
            _formatter = formatter;
            Type = typeof(T);
            TableAlias = GetTableAlias();

            if (!DbConfigurationFactory.Tables.TryGetValue(Type.FullName!, out _table))
            {
                var dbTable = new DbTable { Name = Type.Name!, Columns = new LinkedList<DbColumn>() };
                dbTable.AddFieldsAsColumns<T>();
                DbConfigurationFactory.Tables.TryAdd(Type.FullName!, dbTable);
                _table = dbTable;
            }

            DbParametersToBind = new LinkedList<DbParameterInfo>();
        }

        public string[][] GetAlias(LambdaExpression predicate)
        {
            string[][] aliasIdentifiers = new string[1][];
            var tableIdentifier = predicate!.Parameters[0]!.Name;
            aliasIdentifiers[0] = new string[] { tableIdentifier!, TableAlias };
            return aliasIdentifiers;
        }

        public string GenerateSelectWhere(in Expression<Func<T, bool>> predicate, in SqlOperation operation, out object filter)
        {
            string sqlText = string.Empty;
            int key = 0;

            if (predicate != null)
            {
                key = CalculateExpressionKey(in predicate, in Type, in operation, _formatter.Provider);
                if (GetCachedValues(in key, predicate, ref sqlText, out filter))
                    return sqlText;
            }

            var stringBuilder = new StringBuilder("SELECT ")
                                    .AppendJoin(',', _table.Columns.Select(x => x.FullDbName).ToArray())
                                    .Append($" FROM {TableName}");

            if (predicate != null)
            {
                stringBuilder.Append(" WHERE ")
                             .Append(WhereClause(predicate!.Body, null, GetAlias(predicate)));
            }

            sqlText = stringBuilder.ToString();

            if (predicate != null)
                EnsureCacheItem(key, sqlText, out filter);
            else
                filter = null;

            return sqlText;
        }

        private bool GetCachedValues(in int key, Expression predicate, ref string sql, out object filter)
        {
            if (DynamicQueryString.TryGet(key, out var value))
            {
                sql = value.Item1;

                if (predicate != null)
                {
                    var valueExtractor = new ExpressionValueExtractor<T>(this);
                    valueExtractor.LoadParameterValues(predicate);
                }

                InstanciateFilter(value.Item2, out filter);

                return true;
            }

            filter = null;
            return false;
        }

        private string WhereClause(Expression expression, MemberInfo member = null, string[][] aliasIdentifiers = null)
        {
            return expression switch
            {
                ConstantExpression constantExpression => HandleConstantExpression(constantExpression, member),
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
                                                       Nullable.GetUnderlyingType(memberExpression.Member.DeclaringType) != null ||
                                                       (memberExpression.Member is FieldInfo fieldInfo && Nullable.GetUnderlyingType(fieldInfo.FieldType) != null) ||
                                                       (memberExpression.Type.IsGenericType &&
                                                       typeof(IEnumerable).IsAssignableFrom(memberExpression.Type)) => HandleMemberExpression(memberExpression, aliasIdentifiers),
                NewExpression newExpression => HandleNewExpression(newExpression),
                NewArrayExpression newArrayExpression => HandleNewArrayExpression(newArrayExpression),
                MemberExpression memberExpression when memberExpression.Type == typeof(bool) || memberExpression.Type == typeof(bool?) => $"{GetColumnName(memberExpression, aliasIdentifiers)} = {(_formatter.Provider == SqlProvider.PostgreSql ? "'1'" : "1")}",
                MethodCallExpression methodCall when SQLGenerator<T>.IsStringContains(methodCall) => HandleStringContains(methodCall, StringOperator.Contains, aliasIdentifiers),
                MethodCallExpression methodCall when SQLGenerator<T>.IsEnumerableContains(methodCall) => HandleEnumerableContains(methodCall, aliasIdentifiers),
                MethodCallExpression methodCall when SQLGenerator<T>.IsEquals(methodCall) => HandleStringContains(methodCall, StringOperator.Equals, aliasIdentifiers),
                MethodCallExpression methodCall when SQLGenerator<T>.IsStartsWith(methodCall) => HandleStringContains(methodCall, StringOperator.StartsWith, aliasIdentifiers),
                MethodCallExpression methodCall when SQLGenerator<T>.IsEndsWith(methodCall) => HandleStringContains(methodCall, StringOperator.EndsWith, aliasIdentifiers),
                MethodCallExpression methodCall when SQLGenerator<T>.IsBetween(methodCall) => HandleBetween(methodCall, aliasIdentifiers),
                MethodCallExpression methodCall when SQLGenerator<T>.IsExists(methodCall) => HandleExists(methodCall),
                LambdaExpression lambdaExpression => WhereClause(lambdaExpression.Body, null, aliasIdentifiers),
                MethodCallExpression _ => throw new NotSupportedException(),
                ListInitExpression _ => throw new NotSupportedException(),
                DynamicExpression _ => throw new NotSupportedException(),
                ConditionalExpression _ => throw new NotSupportedException(),
                GotoExpression _ => throw new NotSupportedException(),
                IndexExpression _ => throw new NotSupportedException(),
                InvocationExpression _ => throw new NotSupportedException(),
                LabelExpression _ => throw new NotSupportedException(),
                LoopExpression _ => throw new NotSupportedException(),
                MemberInitExpression _ => throw new NotSupportedException(),
                SwitchExpression _ => throw new NotSupportedException(),
                TryExpression _ => throw new NotSupportedException(),
                TypeBinaryExpression _ => throw new NotSupportedException(),
                _ => throw new NotImplementedException(),
            };
        }

        #region Write Operations

        public string GenerateUpdate()
        {
            if (_table.Key == null)
                throw new InvalidOperationException($"Key was not found in {_table.DbName ?? _table.Name}");

            var columns = _table.Columns.Where(x => x.Name != _table.Key.Name).Select(x => $"{x.DbName ?? x.Name} = {_formatter.BindVariable}{x.Property.Name!}").ToArray();

            return _formatter.GenerateUpdate(TableNameWithoutAlias, columns, _table.Key.DbName ?? _table.Key.Name, _table.Key.Name);
        }

        public string GenerateInsert<T>(bool returnGenerateId = false) where T : class, new()
        {
            var values = _table.Columns.Where(x => !x.AutoGenerated).Select(c => _formatter.BindVariable + c.Name).ToArray();
            var columns = _table.Columns.Where(x => !x.AutoGenerated).Select(x => x.FullDbName).ToArray();

            return _formatter.GenerateInsert(TableNameWithoutAlias, columns, values, _table.Key, returnGenerateId);
        }

        public string GenerateDelete()
        {
            if (_table.Key == null)
                throw new InvalidOperationException($"Key was not found in {_table.DbName ?? _table.Name}");

            return _formatter.GenerateDelete(TableNameWithoutAlias, _table.Key.DbName ?? _table.Key.Name, _table.Key.Name);
        }

        #endregion Write Operations

        #region Cache management

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCacheItem(int key, string sqlText, out object filter)
        {
#if NETFRAMEWORK || NETSTANDARD
            var dynamicType = BuildType(DbParametersToBind.ToArray());
#else
            var dynamicType = BuildType(new ReadOnlySpan<DbParameterInfo>(DbParametersToBind.ToArray()));
#endif
            DynamicQueryString.Set(key, new ValueTuple<string, Type>(sqlText, dynamicType));
            InstanciateFilter(dynamicType, out filter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InstanciateFilter(Type type, out object filter)
        {
            filter = Activator.CreateInstance(type, DbParametersToBind.Select(x => x.Value).ToArray());
        }

#endregion Cache management

        #region parameter handlers
        public void AddInParam(in Type propertyType, object value, string dbType, out string paramName)
        {
            var counter = DbParametersToBind.Count + 1;
            var parameterName = $"p{counter}";
            paramName = _formatter.BindVariable + parameterName;

            var convertedValue = TypeConversionRegistry.ConvertByType(value, propertyType, _formatter.Provider, true);

            DbParametersToBind.AddLast(new DbParameterInfo(
                parameterName,
                paramName,
                0,
                0,
                ParameterDirection.Input,
                null,
                convertedValue.GetType(),
                0,
                convertedValue
                ));
        }

        public void AddInParam(in PropertyInfo propertyInfo)
        {
            var paramName = _formatter.BindVariable + propertyInfo.Name;
            DbParametersToBind.AddLast(new DbParameterInfo(
                propertyInfo.Name,
                paramName,
                0,
                0,
                ParameterDirection.Input,
                propertyInfo,
                null,
                0,
                null
                ));
        }

        public void AddInParam(in PropertyInfo propertyInfo, out string paramName)
        {
            paramName = _formatter.BindVariable + propertyInfo.Name;
            DbParametersToBind.AddLast(new DbParameterInfo(
                propertyInfo.Name,
                paramName,
                0,
                0,
                ParameterDirection.Input,
                null,
                propertyInfo.PropertyType,
                0,
                null
                ));
        }

        public void AddOutParam(DbColumn column, out string paramBindName)
        {
            var counter = DbParametersToBind.Count + 1;
            var paramName = $"p{counter}";
            paramBindName = _formatter.BindVariable + paramName;
            DbParametersToBind.AddLast(new DbParameterInfo(
                paramName,
                paramBindName,
                0,
                0,
                ParameterDirection.Output,
                null,
                column.Property.PropertyType,
                0,
                null
                ));
        }

        #endregion parameter handlers

        #region Handlers

        private string HandleExists(MethodCallExpression methodCall)
        {
            var lambdaExpression = (LambdaExpression)methodCall.Arguments[0];
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
                where = $"WHERE {WhereClause(lambdaExpression.Body, null, aliasIdentifier)}";
            }

            return $"EXISTS (SELECT 1 FROM {tableName} {internalTableAlias} {where})";
        }

        private string HandleNewArrayExpression(NewArrayExpression newArrayExpression)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < newArrayExpression.Expressions.Count; i++)
            {
                builder.Append(WhereClause(newArrayExpression.Expressions[i]));
                if (i < newArrayExpression.Expressions.Count - 1)
                    builder.Append(',');
            }

            return builder.ToString();
        }

        private string HandleBetween(MethodCallExpression methodCall, string[][] aliasIdentifier = null)
        {
            var expression = ((LambdaExpression)methodCall.Arguments[0]).Body as UnaryExpression;

            var minValue = WhereClause(methodCall.Arguments[1]);
            var maxValue = WhereClause(methodCall.Arguments[2]);

            return $"{WhereClause(expression.Operand, null, aliasIdentifier)} BETWEEN {minValue} AND {maxValue}";
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

                AddInParam(constant.Type, constant.Value, null, out var paramName);
                return paramName;
            }
            else if (memberExpression.Expression is MemberExpression)
            {
                return WhereClause(memberExpression.Expression, memberExpression.Member, aliasIdentifier);
            }

            return GetColumnName(memberExpression, aliasIdentifier);
        }

        private static IEnumerable<T> GetValues<T>(ConstantExpression expression)
        {
            var instantiator = Expression.Lambda<Func<IEnumerable<T>>>(expression).Compile();
            return instantiator();
        }

        private void AddArrayConstantValues<T>(Type type, ConstantExpression expression, HashSet<string> @params)
        {
            foreach (var value in GetValues<T>(expression))
            {
                AddInParam(type, value, null, out var paramName);
                @params.Add(paramName);
            }
        }

        private string HandleNewExpression(NewExpression newExpression)
        {
            if (newExpression.Type == typeof(DateTime))
            {
                var instantiator = Expression.Lambda<Func<DateTime>>(newExpression).Compile();
                var value = instantiator();

                AddInParam(typeof(DateTime), value, null, out var paramName);

                return paramName;
            }

            return "";
        }

        private string HandleConstantExpression(ConstantExpression constantExpression, MemberInfo member)
        {
            if (constantExpression.Value == null)
                return "IS NULL";

            string paramName;
            if (member != null && member is FieldInfo fieldInfo)
            {
                var value = fieldInfo.GetValue(constantExpression.Value);
                AddInParam(fieldInfo.FieldType, value, null, out paramName);
            }
            else
            {
                AddInParam(constantExpression.Type, constantExpression.Value, null, out paramName);
            }

            return paramName;
        }

        private string HandleUnaryExpression(UnaryExpression expression, string[][] aliasIdentifiers = null)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Not:
                    var operand = WhereClause(expression.Operand, null, aliasIdentifiers);
                    return $"NOT ({operand})";
                case ExpressionType.Convert:
                    return WhereClause(expression.Operand, null, aliasIdentifiers);
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
                left = WhereClause(expression.Left, null, aliasIdentifiers);
                right = WhereClause(expression.Right, null, aliasIdentifiers);
            }
            else
            {
                left = WhereClause(expression.Left, null, aliasIdentifiers);

                if (expression.Right is ConstantExpression constantExpression && constantExpression.Value == null)
                {
                    if (expression.NodeType == ExpressionType.Equal)
                        return $"{left} IS NULL";
                    else if (expression.NodeType == ExpressionType.NotEqual)
                        return $"{left} IS NOT NULL";
                }

                right = WhereClause(expression.Right, null, aliasIdentifiers);

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
                AddInParam(typeof(string), value, null, out var varName);
                var likeSQL = _formatter.Concatenate($"'{initialOperator}'", varName, $"'{finalOperator}'");
                return $"{column} LIKE {likeSQL}";
            }
            else if (expression.Object is MemberExpression memberExpression2
                && expression.Arguments[0] is ConstantExpression constantExpression2
                && @operator == StringOperator.Equals)
            {
                var column = GetColumnName(memberExpression2, aliasIdentifiers);
                var value = WhereClause(constantExpression2, memberExpression2.Member, aliasIdentifiers);
                return $"{column} = {value}";
            }
            else if (expression.Object is MemberExpression memberExpression3 &&
                expression.Arguments[0] is MemberExpression memberExpression4 &&
                memberExpression4.Member is FieldInfo fieldInfo)
            {
                var column = GetColumnName(memberExpression3, aliasIdentifiers);
                var constant = memberExpression4.Expression as ConstantExpression;
                var value = fieldInfo.GetValue(constant.Value);
                var initialOperator = @operator == StringOperator.StartsWith ? "" : "%";
                var finalOperator = @operator == StringOperator.EndsWith ? "" : "%";
                AddInParam(typeof(string), value, null, out var varName);
                var likeSQL = _formatter.Concatenate($"'{initialOperator}'", varName, $"'{finalOperator}'");
                return $"{column} LIKE {likeSQL}";
            }

            throw new NotSupportedException("Unsupported method call");
        }

        private string HandleEnumerableContains(MethodCallExpression expression, string[][] aliasIdentifiers = null)
        {
            if (expression.Arguments.Count == 2)
            {
                var values = WhereClause(expression.Arguments[0], null, aliasIdentifiers);
                var column = WhereClause(expression.Arguments[1], null, aliasIdentifiers);
                return $"{column} IN ({values})";
            }
            else if (expression.Arguments.Count == 1)
            {
                var values = WhereClause(expression.Object, null, aliasIdentifiers);
                var column = WhereClause(expression.Arguments[0], null, aliasIdentifiers);
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

        private int indexTableAlias;

        private string GetTableAlias()
        {
#if NETCOREAPP
            return "ABCDEFGHIJKLMNOPQRSTUVWXYZ".AsSpan().Slice(indexTableAlias++, 1).ToString();
#else
            return "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring(indexTableAlias++, 1);
#endif
        }

        private string GetColumnName(MemberExpression member, string[][] aliasIdentifiers = null)
        {
            if (member.Expression is ConstantExpression)
                return WhereClause(member.Expression, member.Member, aliasIdentifiers);

            var dbColumn = _table.Columns.First(x => x.Name == member.Member.Name);

            if (member.Member.ReflectedType == typeof(T))
                return $"{TableAlias}.{dbColumn.DbName ?? dbColumn.Name}";

            var tableTempAlias = member.Expression!.ToString().Split('.')[0];
            return $"{ReplaceIdentifier(tableTempAlias, aliasIdentifiers)}.{dbColumn.DbName ?? dbColumn.Name}";
        }
        private static string ReplaceIdentifier(string expression, string[][] aliasIdentifiers)
        {
            for (int i = 0; i < aliasIdentifiers.Length; i++)
            {
                if (aliasIdentifiers[i][0] == expression)
                {
                    return aliasIdentifiers[i][1];
                }
            }

            return expression;
        }
        #endregion Handlers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CalculateExpressionKey(in Expression<Func<T, bool>> expression, in Type type, in SqlOperation operation, in SqlProvider provider, in string key = null)
        {
            if (!string.IsNullOrEmpty(key))
            {
                return key.GetHashCode();
            }

            int calculatedKey = 17;

            unchecked
            {
                calculatedKey = (calculatedKey * 23) + operation.GetHashCode();
                calculatedKey = (calculatedKey * 23) + provider.GetHashCode();
                calculatedKey = (calculatedKey * 23) + type.GetHashCode();
                calculatedKey = (calculatedKey * 23) + ExpressionHasher.GetHashCode(expression!, provider);
            }

            return calculatedKey;
        }
    }

    internal enum SqlOperation : byte
    {
        SelectList = 0x1,
        SelectSingle = 0x2,
        Insert = 0x3,
        Update = 0x4,
        Delete = 0x5
    }

    internal enum StringOperator : byte
    {
        Contains = 0,
        Equals = 1,
        StartsWith = 2,
        EndsWith = 3
    }
}
