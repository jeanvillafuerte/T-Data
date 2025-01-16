using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using TData.InternalCache;
using TData.Configuration;
using TData.Core.Converters;
using TData.Core.FluentApi;
using TData.Core.Provider;
using TData.Helpers;

[assembly: InternalsVisibleTo("TData.Cache")]

namespace TData.Core.QueryGenerator
{
    internal interface IParameterHandler
    {
        void AddInParam(in Type propertyType, object value, string dbType, out string paramName);
    }

    internal class SQLGenerator<T> : IParameterHandler
    {
        protected readonly string TableAlias;
        private readonly Type Type;
        private string DbSchema
        {
            get
            {
                if (Table.Schema != null)
                    return $"{Table.Schema}.";
                return string.Empty;
            }
        }

        private string TableName
        {
            get
            {
                return $"{DbSchema}{Table.DbName ?? Table.Name} {TableAlias}";
            }
        }

        private string TableNameWithoutAlias
        {
            get
            {
                return DbSchema + (Table.DbName ?? Table.Name);
            }
        }

        private bool _isStaticQuery = true;
        protected readonly DbTable Table;
        private readonly ISqlFormatter Formatter;
        private readonly bool Buffered;

        /// <summary>
        /// considering is an arbitrary amount of parameters, a linked list is the best option
        /// </summary>
        internal readonly LinkedList<DbParameterInfo> DbParametersToBind;

        public SQLGenerator(in ISqlFormatter formatter) 
        {
            Formatter = formatter;
        }

        public SQLGenerator(in ISqlFormatter formatter, in bool buffered)
        {
            Buffered = buffered;
            Formatter = formatter;
            Type = typeof(T);

            if (!DbConfig.Tables.TryGetValue(Type.FullName!, out Table))
            {
                var dbTable = new DbTable { Name = Type.Name!, Columns = new LinkedList<DbColumn>() };
                dbTable.AddFieldsAsColumns<T>();
                DbConfig.Tables.TryAdd(Type.FullName!, dbTable);
                Table = dbTable;
            }

            TableAlias = formatter.Provider == DbProvider.Sqlite ? TableNameWithoutAlias : GetTableAlias();
            DbParametersToBind = new LinkedList<DbParameterInfo>();
        }

        #region main selector handlers

        internal string[][] GetAlias(LambdaExpression predicate)
        {
            string[][] aliasIdentifiers = new string[1][];
            var tableIdentifier = predicate!.Parameters[0]!.Name;
            aliasIdentifiers[0] = new string[] { tableIdentifier!, TableAlias };
            return aliasIdentifiers;
        }

        internal string GeneratePagingQuery(in string query)
        {
            return Formatter.PagingQuery(query);
        }

        internal string GenerateSelect(in Expression<Func<T, bool>> predicate, in Expression<Func<T, object>> selector, in SqlOperation operation, out object[] filter)
        {
            string sqlText = string.Empty;
            int? keyValue = null;

            if (predicate != null && Buffered)
            {
                var key = CalculateExpressionKey(in predicate, in selector, null, in Type, in operation, Formatter.Provider);
                if (GetCachedValues(in key, predicate, ref sqlText, out filter))
                    return sqlText;
                else
                    keyValue = key;
            }

            var stringBuilder = new StringBuilder("SELECT ");

            if (selector == null)
                stringBuilder.Append('*');
            else
                stringBuilder.Append(GetSelectedColumns(selector));

            stringBuilder.Append($" FROM {TableName}");

            if (predicate != null)
            {
                stringBuilder.Append(" WHERE ")
                             .Append(WhereClause(predicate!.Body, null, GetAlias(predicate)));
            }

            sqlText = stringBuilder.ToString();

            if (predicate != null)
            {
                EnsureCacheInfo(key: keyValue, sqlText, buffered: Buffered);
                filter = DbParametersToBind.ToArray();
            }
            else
                filter = null;

            return sqlText;
        }

        private bool GetCachedValues(in int key, Expression predicate, ref string sql, out object[] values)
        {
            if (DynamicQueryInfo.TryGet(key, out var value))
            {
                sql = value.Query;

                if (predicate != null && !value.IsStaticQuery)
                {
                    var valueExtractor = new ExpressionValueExtractor<T>(this);
                    valueExtractor.LoadParameterValues(predicate);
                    values = DbParametersToBind.Select(x => x.Value).ToArray();
                }
                else
                {
                    values = value.ParameterValues;
                }
                return true;
            }

            values = null;
            return false;
        }

        private string GetSelectedColumns(Expression<Func<T, object>> selector)
        {
            var newExpression = selector.Body as NewExpression;
            var columns = new List<string>();

            foreach (var member in newExpression.Members)
            {
                var column = Table.Columns.First(x => x.Name == member.Name);
                columns.Add($"{TableAlias}.{column.DbName ?? column.Name}");
            }

            return string.Join(",", columns);
        }

        private DbColumn GetSelectedColumn(Expression<Func<T, object>> selector)
        {
            if (selector is LambdaExpression lambdaExpression && !lambdaExpression.CanReduce)
            {
                //those fields that need cast to object
                if (lambdaExpression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression memberExpression)
                    return Table.Columns.First(x => x.Name == memberExpression.Member.Name);
                //others like string or non-enum types
                if (lambdaExpression.Body is MemberExpression memberExpression2)
                    return Table.Columns.First(x => x.Name == memberExpression2.Member.Name);

            }
            
             throw new NotSupportedException("Unsupported selector");
        }

        #endregion

        private string WhereClause(in Expression expression, in MemberInfo member = null, in string[][] aliasIdentifiers = null)
        {
            return expression switch
            {
                ConstantExpression constantExpression => HandleConstantExpression(in constantExpression, in member),
                UnaryExpression unaryExpression => HandleUnaryExpression(in unaryExpression, in aliasIdentifiers),
                BinaryExpression binaryExpression => HandleBinaryExpression(in binaryExpression, in aliasIdentifiers),
                MemberExpression memberExpression when memberExpression.Member.Name == "Now" && memberExpression.Type == typeof(DateTime) => SqlDateNow(),
                MemberExpression memberExpression when memberExpression.Member.Name == "MinValue" && memberExpression.Type == typeof(DateTime) => SqlMin(),
                MemberExpression memberExpression when memberExpression.Member.Name == "MaxValue" && memberExpression.Type == typeof(DateTime) => SqlMax(),
                MemberExpression memberExpression when (memberExpression.Type == typeof(string) || Nullable.GetUnderlyingType(memberExpression.Type) != null) => GetColumnName(in memberExpression, in aliasIdentifiers),
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
                                                       typeof(IEnumerable).IsAssignableFrom(memberExpression.Type)) => HandleMemberExpression(in memberExpression, in aliasIdentifiers),
                NewExpression newExpression => HandleNewExpression(in newExpression),
                NewArrayExpression newArrayExpression => HandleNewArrayExpression(in newArrayExpression),
                MemberExpression memberExpression when memberExpression.Type == typeof(bool) => $"{GetColumnName(in memberExpression, in aliasIdentifiers)} = {(Formatter.Provider == DbProvider.PostgreSql ? "'1'" : "1")}",
                MethodCallExpression methodCall when IsStringContains(methodCall) => HandleStringContains(in methodCall, StringOperator.Contains, in aliasIdentifiers),
                MethodCallExpression methodCall when IsEnumerableContains(methodCall) => HandleEnumerableContains(in methodCall, in aliasIdentifiers),
                MethodCallExpression methodCall when IsEquals(methodCall) => HandleStringContains(in methodCall, StringOperator.Equals, in aliasIdentifiers),
                MethodCallExpression methodCall when IsStartsWith(methodCall) => HandleStringContains(in methodCall, StringOperator.StartsWith, in aliasIdentifiers),
                MethodCallExpression methodCall when IsEndsWith(methodCall) => HandleStringContains(in methodCall, StringOperator.EndsWith, in aliasIdentifiers),
                MethodCallExpression methodCall when IsBetween(methodCall) => HandleBetween(in methodCall, in aliasIdentifiers),
                MethodCallExpression methodCall when IsExists(methodCall) => HandleExists(in methodCall),
                LambdaExpression lambdaExpression => WhereClause(lambdaExpression.Body, null, in aliasIdentifiers),
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

        internal string GenerateUpdate()
        {
            if (Table.Key == null)
                throw new InvalidOperationException($"Key was not found in {Table.DbName ?? Table.Name}");

            var columns = Table.Columns.Where(x => x.Name != Table.Key.Name).Select(x => $"{x.DbName ?? x.Name} = {Formatter.BindVariable}{x.Property.Name!}").ToArray();
            return new StringBuilder($"UPDATE {TableNameWithoutAlias} SET ")
                               .AppendJoin(',', columns)
                               .Append($" WHERE {Table.Key.DbName ?? Table.Key.Name} = {Formatter.BindVariable}{Table.Key.Name}").ToString();
        }

        internal string GenerateUpdate(in Expression<Func<T, bool>> predicate, out object[] filter, params (Expression<Func<T, object>> field, object value)[] updates)
        {
            string updateQueryText = string.Empty;
            int? keyValue = null;
            
            if (Buffered)
            {
                int key = CalculateExpressionKey(in predicate, null, in updates, in Type, SqlOperation.Update, Formatter.Provider);

                if (GetCachedValues(in key, predicate, ref updateQueryText, out filter))
                {
                    return updateQueryText;
                }
                else
                    keyValue = key;
            }

            var columns = new string[updates.Length];
            for (int i = 0; i < updates.Length; i++)
            {
                var column = GetSelectedColumn(updates[i].field);
                AddInParam(column.Property.PropertyType, updates[i].value, null, out var paramName);
                columns[i] = $"{column.DbName ?? column.Name} = {paramName}";
            }

            StringBuilder stringBuilder = null;

            if (Formatter.Provider == DbProvider.SqlServer)
            {
                stringBuilder = new StringBuilder($"UPDATE {TableAlias} SET ")
                                                   .AppendJoin(',', columns)
                                                   .Append($" FROM {TableNameWithoutAlias} {TableAlias}");
            }
            else
            {
                var tableAlias = Formatter.Provider == DbProvider.Sqlite ? "" : TableAlias;
                stringBuilder = new StringBuilder($"UPDATE {TableNameWithoutAlias} {tableAlias} SET ")
                                           .AppendJoin(',', columns);
            }

            stringBuilder.Append(" WHERE ")
                               .Append(WhereClause(predicate.Body, null, GetAlias(predicate))).ToString();

            updateQueryText = stringBuilder.ToString();
            EnsureCacheInfo(key: keyValue, updateQueryText, buffered: Buffered);
            filter = DbParametersToBind.ToArray();

            return updateQueryText;
        }

        internal string GenerateInsert(bool generateKeyValue = false)
        {
            var values = Table.Columns.Where(x => !x.AutoGenerated).Select(c => Formatter.BindVariable + c.Name).ToArray();
            var columns = Table.Columns.Where(x => !x.AutoGenerated).Select(x => x.DbName ?? x.Name).ToArray();

            return Formatter.GenerateInsert(TableNameWithoutAlias, columns, values, Table.Key, generateKeyValue);
        }

        internal string GenerateDelete()
        {
            if (Table.Key == null)
                throw new InvalidOperationException($"Key was not found in {Table.DbName ?? Table.Name}");

            var header = Formatter.GenerateDelete(TableNameWithoutAlias, TableAlias);
            return $"{header} WHERE {Table.Key.DbName ?? Table.Key.Name} = {Formatter.BindVariable}{Table.Key.Name}";
        }

        internal string GenerateDelete(in Expression<Func<T, bool>> predicate, in SqlOperation operation, out object[] filter)
        {
            string sqlText = string.Empty;
            int? keyValue = null;

            if (Buffered)
            {
                var key = CalculateExpressionKey(in predicate, null, null, in Type, in operation, Formatter.Provider);
                if (GetCachedValues(in key, predicate, ref sqlText, out filter))
                    return sqlText;
                else
                    keyValue = key;
            }

            var stringBuilder = new StringBuilder(Formatter.GenerateDelete(TableNameWithoutAlias, TableAlias));

            stringBuilder.Append(" WHERE ")
                         .Append(WhereClause(predicate!.Body, null, GetAlias(predicate)));

            sqlText = stringBuilder.ToString();

            EnsureCacheInfo(key: keyValue, sqlText, buffered: Buffered);
            filter = DbParametersToBind.ToArray();
            return sqlText;
        }

        internal string GetTableName()
        {
            return TableNameWithoutAlias;
        }


        #endregion Write Operations

        #region Cache management

        private void EnsureCacheInfo(int? key = null, in string sqlText = null, in bool buffered = true)
        {
            var paramValues = DbParametersToBind.Select(x => x.Value).ToArray();

            if (buffered && key.HasValue)
                DynamicQueryInfo.Set(key.Value, new ExpressionQueryItem(in sqlText, in _isStaticQuery, _isStaticQuery ? paramValues : null));
        }

#endregion Cache management

        #region parameter handlers
        protected void AddInParam(DbColumn column, object value, out string paramName)
        {
            AddInParam(column.Property.PropertyType, value, null, out paramName);
        }

        public void AddInParam(in Type propertyType, object value, string dbType, out string paramName)
        {
            var counter = DbParametersToBind.Count + 1;
            var parameterName = $"p{counter}";
            paramName = Formatter.BindVariable + parameterName;

            var convertedValue = TypeConversionRegistry.ConvertInParameterValue(Formatter.Provider, value, in propertyType, true);

            DbParametersToBind.AddLast(new DbParameterInfo(
                parameterName,
                paramName,
                0,
                propertyType == typeof(decimal) ? 18 : 0,
                propertyType == typeof(decimal) ? 6 : 0,
                ParameterDirection.Input,
                null,
                convertedValue.GetType(),
                DatabaseHelperProvider.GetEnumValue(Formatter.Provider, in propertyType),
                convertedValue,
                null));
        }

        #endregion parameter handlers

        #region Handlers

        private string HandleExists(in MethodCallExpression methodCall)
        {
            var lambdaExpression = (LambdaExpression)methodCall.Arguments[0];
            var internalTableAlias = GetTableAlias();
            var a = lambdaExpression.Parameters[0];
            var b = lambdaExpression.Parameters[1];

            string where = "";

            if (!DbConfig.Tables.TryGetValue(b.Type.FullName!, out var _table))
            {
                var dbTable = new DbTable { Name = Type.Name!, Columns = new LinkedList<DbColumn>() };
                dbTable.AddFieldsAsColumns<T>();
                DbConfig.Tables.TryAdd(Type.FullName!, dbTable);
                _table = dbTable;
            }

            string tableName = DbSchema + (_table.DbName ?? _table.Name);

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
                where = $"WHERE {WhereClause(lambdaExpression.Body, null, in aliasIdentifier)}";
            }

            return $"EXISTS (SELECT 1 FROM {tableName} {internalTableAlias} {where})";
        }

        private string HandleNewArrayExpression(in NewArrayExpression newArrayExpression)
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

        private string HandleBetween(in MethodCallExpression methodCall, in string[][] aliasIdentifier = null)
        {
            var expression = ((LambdaExpression)methodCall.Arguments[0]).Body as UnaryExpression;

            var minValue = WhereClause(methodCall.Arguments[1]);
            var maxValue = WhereClause(methodCall.Arguments[2]);

            return $"{WhereClause(expression.Operand, null, in aliasIdentifier)} BETWEEN {minValue} AND {maxValue}";
        }

        private string SqlDateNow() => Formatter.CurrentDate;
        private string SqlMin() => Formatter.MinDate;
        private string SqlMax() => Formatter.MaxDate;

        private string HandleMemberExpression(in MemberExpression memberExpression, in string[][] aliasIdentifier = null)
        {
            if (memberExpression.Expression is ConstantExpression ce && memberExpression.Member is FieldInfo fe)
            {
                _isStaticQuery = false;
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
                return WhereClause(memberExpression.Expression, memberExpression.Member, in aliasIdentifier);
            }

            return GetColumnName(in memberExpression, in aliasIdentifier);
        }

        private static IEnumerable<TE> GetValues<TE>(ConstantExpression expression)
        {
            var instantiator = Expression.Lambda<Func<IEnumerable<TE>>>(expression).Compile();
            return instantiator();
        }

        private void AddArrayConstantValues<TValue>(Type type, ConstantExpression expression, HashSet<string> @params)
        {
            //TODO: if the account of values is too big consider generate a temp table
            foreach (var value in GetValues<TValue>(expression))
            {
                AddInParam(type, value, null, out var paramName);
                @params.Add(paramName);
            }

            if (@params.Count() >= 1000)
                throw new NotSupportedException("The amount of values in the array is too big for IN expression");
        }

        private string HandleNewExpression(in NewExpression newExpression)
        {
            if (newExpression.Type == typeof(DateTime))
            {
                var instantiator = Expression.Lambda<Func<DateTime>>(newExpression).Compile();
                var value = instantiator();

                AddInParam(typeof(DateTime), value, null, out var paramName);

                return ApplyTransformation(paramName, nameof(DateTime));
            }

            return "";
        }

        private string HandleConstantExpression(in ConstantExpression constantExpression, in MemberInfo member)
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

        private string HandleUnaryExpression(in UnaryExpression expression, in string[][] aliasIdentifiers = null)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Not:
                    var operand = WhereClause(expression.Operand, null, in aliasIdentifiers);
                    return $"NOT ({operand})";
                case ExpressionType.Convert:
                    return WhereClause(expression.Operand, null, in aliasIdentifiers);
                default:
                    throw new NotSupportedException("Unsupported unary operator");
            }
        }

        private string HandleBinaryExpression(in BinaryExpression expression, in string[][] aliasIdentifiers = null)
        {
            string left;
            string right;
            if (expression.Left is MemberExpression && expression.Right is MemberExpression)
            {
                left = WhereClause(expression.Left, null, in aliasIdentifiers);
                right = WhereClause(expression.Right, null, in aliasIdentifiers);
            }
            else
            {
                left = WhereClause(expression.Left, null, in aliasIdentifiers);

                if (expression.Right is ConstantExpression constantExpression && constantExpression.Value == null)
                {
                    if (expression.NodeType == ExpressionType.Equal)
                        return $"{left} IS NULL";
                    else if (expression.NodeType == ExpressionType.NotEqual)
                        return $"{left} IS NOT NULL";
                }

                right = WhereClause(expression.Right, null, in aliasIdentifiers);

                if (expression.Left is ConstantExpression constantExpression2 && constantExpression2.Value == null)
                {
                    if (expression.NodeType == ExpressionType.Equal)
                        return $"{right} IS NULL";
                    else if (expression.NodeType == ExpressionType.NotEqual)
                        return $"{right} IS NOT NULL";
                }
            }

            return Formatter.FormatOperator(left, right, expression.NodeType);
        }

        private string HandleStringContains(in MethodCallExpression expression, in StringOperator @operator, in string[][] aliasIdentifiers = null)
        {
            if (expression.Object is MemberExpression memberExpression
                && expression.Arguments[0] is ConstantExpression constantExpression && @operator != StringOperator.Equals)
            {
                var column = GetColumnName(in memberExpression, in aliasIdentifiers);
                var value = constantExpression.Value.ToString();
                var initialOperator = @operator == StringOperator.StartsWith ? "" : "%";
                var finalOperator = @operator == StringOperator.EndsWith ? "" : "%";
                AddInParam(typeof(string), value, null, out var varName);
                var likeSQL = Formatter.Concatenate($"'{initialOperator}'", varName, $"'{finalOperator}'");
                return $"{column} LIKE {likeSQL}";
            }
            else if (expression.Object is MemberExpression memberExpression2
                && expression.Arguments[0] is ConstantExpression constantExpression2
                && @operator == StringOperator.Equals)
            {
                var column = GetColumnName(in memberExpression2, in aliasIdentifiers);
                var value = WhereClause(constantExpression2, memberExpression2.Member, aliasIdentifiers);
                return $"{column} = {value}";
            }
            else if (expression.Object is MemberExpression memberExpression3 &&
                expression.Arguments[0] is MemberExpression memberExpression4 &&
                memberExpression4.Member is FieldInfo fieldInfo)
            {
                var column = GetColumnName(in memberExpression3, in aliasIdentifiers);
                var constant = memberExpression4.Expression as ConstantExpression;
                var value = fieldInfo.GetValue(constant.Value);
                var initialOperator = @operator == StringOperator.StartsWith ? "" : "%";
                var finalOperator = @operator == StringOperator.EndsWith ? "" : "%";
                AddInParam(typeof(string), value, null, out var varName);
                var likeSQL = Formatter.Concatenate($"'{initialOperator}'", varName, $"'{finalOperator}'");
                return $"{column} LIKE {likeSQL}";
            }

            throw new NotSupportedException("Unsupported method call");
        }

        private string HandleEnumerableContains(in MethodCallExpression expression, in string[][] aliasIdentifiers = null)
        {
            if (expression.Arguments.Count == 2)
            {
                var values = WhereClause(expression.Arguments[0], null, in aliasIdentifiers);
                var column = WhereClause(expression.Arguments[1], null, in aliasIdentifiers);
                return $"{column} IN ({values})";
            }
            else if (expression.Arguments.Count == 1)
            {
                var values = WhereClause(expression.Object, null, in aliasIdentifiers);
                var column = WhereClause(expression.Arguments[0], null, in aliasIdentifiers);
                return $"{column} IN ({values})";
            }

            return "";
        }

        internal static bool IsBetween(in MethodCallExpression expression) =>
                     expression.Method.DeclaringType.Name == "SqlExpression"
                           && expression.Method.Name == "Between";

        internal static bool IsExists(in MethodCallExpression expression) =>
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

        private string GetColumnName(in MemberExpression member, in string[][] aliasIdentifiers = null)
        {
            if (member.Expression is ConstantExpression)
                return WhereClause(member.Expression, member.Member, in aliasIdentifiers);

            if (member.Expression != null)
            {
                DbColumn dbColumn = null;
                foreach (var column in Table.Columns)
                {
                    if (column.Name == member.Member.Name)
                    {
                        dbColumn = column;
                        break;
                    }
                }

                if (member.Member.ReflectedType == typeof(T))
                    return ApplyTransformation($"{(aliasIdentifiers == null ? "" : $"{TableAlias}.")}{dbColumn.DbName ?? dbColumn.Name}", member.Type.Name);

                var tableTempAlias = member.Expression!.ToString().Split('.')[0];
                return $"{ReplaceIdentifier(tableTempAlias, aliasIdentifiers)}.{dbColumn.DbName ?? dbColumn.Name}";
            }
            else
            {
                //handle static values from dotnet libraries
                return member.Member.Name;
            }
        }

        private string ApplyTransformation(string name, string typeName)
        {
            if (Formatter.Provider == DbProvider.Sqlite)
            {
                return typeName switch
                {
                    "DateTime" => $"datetime({name})",
                    _ => name,
                };
            }
            return name;
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

        internal static int CalculateExpressionKey(in Expression<Func<T, bool>> expression, in Expression<Func<T, object>> selector, in (Expression<Func<T, object>> field, object value)[] updates, in Type type, in SqlOperation operation, in DbProvider provider, in string key = null)
        {
            if (!string.IsNullOrEmpty(key))
            {
                return HashHelper.GenerateHash(key);
            }

            unchecked
            {
                int calculatedKey = 17;
                calculatedKey = (calculatedKey * 23) + operation.GetHashCode();
                calculatedKey = (calculatedKey * 23) + provider.GetHashCode();
                calculatedKey = (calculatedKey * 23) + type.GetHashCode();

                if (expression != null)
                    calculatedKey = (calculatedKey * 23) + ExpressionHasher.GetPredicateHashCode(in expression, in provider);

                if (selector != null)
                {
                    var newExpression = selector.Body as NewExpression;
                    foreach (var member in newExpression.Members)
                        calculatedKey = (calculatedKey * 23) + member.GetHashCode();
                }

                if (updates != null)
                {
                    foreach (var (field, _) in updates)
                    {
                        if (field is LambdaExpression lambdaExpression && !lambdaExpression.CanReduce)
                        {
                            if (lambdaExpression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression memberExpression)
                                calculatedKey = (calculatedKey * 23) + memberExpression.Member.GetHashCode();
                            if (lambdaExpression.Body is MemberExpression memberExpression2)
                                calculatedKey = (calculatedKey * 23) + memberExpression2.Member.GetHashCode();
                        }
                    }
                }

                return calculatedKey;
            }
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
