using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Thomas.Database.Helpers
{
    internal static class ExpressionHasher
    {
        public static int GetHashCode<T>(Expression<Func<T, bool>> expr, SqlProvider provider, bool includeValues = true)
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

                    return (hash * 23) + int.Parse(value.ToString("yyyyMMddmmss"));
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

        public static int MemberExpression(MemberExpression memberExpr, int hash, bool includeValues, SqlProvider provider)
        {
            hash = (hash * 23) + memberExpr.Member.Name.GetHashCode();

            if (memberExpr.Expression != null)
                hash = (hash * 23) + GetHashCode(memberExpr.Expression, hash, includeValues, provider);

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
