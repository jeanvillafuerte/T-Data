using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Thomas.Database.Helpers
{
    internal static class ExpressionHasher
    {
        internal static int GetSelectorHashCode<T>(in Expression<Func<T, object>> selector, in SqlProvider provider)
        {
            var newExpression = selector.Body as NewExpression;

            if (newExpression == null)
                throw new NotSupportedException("Selector must be a NewExpression");

            unchecked
            {
                int hash = 17;

                foreach (var member in newExpression.Members)
                    hash = (hash * 23) + member.GetHashCode();

                return hash;
            }
        }

        public static int GetPredicateHashCode<T>(in Expression<Func<T, bool>> expr, in SqlProvider provider, in bool includeValues = true)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + provider.GetHashCode();
                hash = (hash * 23) + expr.Body.NodeType.GetHashCode();
                return GetHashCode(expr.Body, hash, includeValues, provider);
            }
        }

        private static int GetHashCode(Expression expr, int hash, bool includeValues, SqlProvider provider, MemberInfo member = null)
        {
            return expr switch
            {
                ConstantExpression constExpr => ConstantExpression(constExpr, member, hash, includeValues),
                UnaryExpression unaryExpr => (hash * 23) + GetHashCode(unaryExpr.Operand, hash, includeValues, provider, member),
                BinaryExpression binExpr => BinaryExpressionHash(binExpr, hash, includeValues, provider),
                MemberExpression memberExpr => MemberExpression(memberExpr, hash, includeValues, provider),
                MethodCallExpression methodExpr => MethodCallExpression(methodExpr, hash, includeValues, provider),
                LambdaExpression lambdaExpr => LambdaExpression(lambdaExpr, hash, includeValues, provider),
                NewArrayExpression newArrayExpression => newArrayExpression.Expressions.Aggregate(hash, (current, expression) => GetHashCode(expression, current, includeValues, provider)),
                ParameterExpression parameterExpression => hash * 397 + parameterExpression.Name.GetHashCode(),
                NewExpression newExpression => HandleNewExpression(newExpression, hash, includeValues),
                _ => throw new NotImplementedException(),
            };
        }

        private static int HandleNewExpression(NewExpression newExpression, int hash, bool includeValues)
        {
            if (newExpression.Type == typeof(DateTime))
            {
                if (includeValues)
                {
                    var instantiator = Expression.Lambda<Func<DateTime>>(newExpression).Compile();
                    var value = instantiator();

                    unchecked
                    {
                        return (hash * 23) + value.GetHashCode();
                    }
                }

                return (hash * 23) + newExpression.Type.GetHashCode();
            }

            return hash;
        }

        private static int ConstantExpression(ConstantExpression constExpr,MemberInfo member, int hash, bool includeValues)
        {
            if (includeValues)
                return (hash * 23) + constExpr.Type.GetHashCode();

            hash = member != null && member is FieldInfo fieldInfo
                ? (hash * 23) + fieldInfo.Name.GetHashCode()
                : (hash * 23) + constExpr.Type.GetHashCode();

            return hash;
        }

        private static int BinaryExpressionHash(BinaryExpression binExpr, int hash, bool includeValues, SqlProvider provider)
        {
            var tempHash = GetHashCode(binExpr.Left, hash, includeValues, provider);
            hash = (hash * 23) + tempHash;
            tempHash = GetHashCode(binExpr.Right, hash, includeValues, provider);
            hash = (hash * 23) + tempHash;
            return hash;
        }

        public static int MemberExpression(MemberExpression memberExpression, int hash, bool includeValues, SqlProvider provider)
        {
            hash = (hash * 23) + memberExpression.Member.Name.GetHashCode();

            if (memberExpression.Expression is ConstantExpression ce && memberExpression.Member is FieldInfo fe)
            {
                var constant = Expression.Constant(fe.GetValue(ce.Value));
                if (constant.Type.IsArray || (constant.Type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(constant.Type)))
                {
                    var elementAmount = ((IEnumerable)constant.Value).Cast<object>().ToArray().Length;
                    return (hash * 23) + elementAmount;
                }

                return (hash * 23) + constant.Value.GetHashCode();
            }
            
            if (memberExpression.Expression != null)
                hash = (hash * 23) + GetHashCode(memberExpression.Expression, hash, includeValues, provider);

            return hash;
        }

        public static int MethodCallExpression(MethodCallExpression methodExpr, int hash, bool includeValues, SqlProvider provider)
        {
            hash = (hash * 23) + methodExpr.Method.Name.GetHashCode();
            foreach (var arg in methodExpr.Arguments)
            {
                if (arg is ConstantExpression expression)
                    hash = (hash * 23) + provider.GetHashCode();
                else
                    hash = hash * 23 + GetHashCode(arg, hash, includeValues, provider);
            }
            return hash;
        }

        public static int LambdaExpression(LambdaExpression lambdaExpr, int hash, bool includeValues, SqlProvider provider)
        {
            hash = hash * 23 + GetHashCode(lambdaExpr.Body, hash, includeValues, provider);
            foreach (var p in lambdaExpr.Parameters)
            {
                hash = hash * 23 + p.Name.GetHashCode();
            }
            return hash;
        }

    }
}
