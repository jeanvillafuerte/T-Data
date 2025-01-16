using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TData.Helpers
{
    internal static class ExpressionHasher
    {
        public static int GetPredicateHashCode<T>(in Expression<Func<T, bool>> expr, in DbProvider provider, in bool includeValues = true)
        {
            int hash = 17;
            hash = (hash * 23) + provider.GetHashCode();
            hash = (hash * 23) + expr.Body.NodeType.GetHashCode();
            return GetHashCode(expr.Body, in hash, in includeValues, in provider);
        }

        private static int GetHashCode(in Expression expr, in int hash, in bool includeValues, in DbProvider provider, in MemberInfo member = null)
        {
            return expr switch
            {
                ConstantExpression constExpr => ConstantExpression(in constExpr, in member, hash, in includeValues),
                UnaryExpression unaryExpr => (hash * 23) + GetHashCode(unaryExpr.Operand, in hash, in includeValues, in provider, in member),
                BinaryExpression binExpr => BinaryExpressionHash(in binExpr, hash, in includeValues, in provider),
                MemberExpression memberExpr => MemberExpression(in memberExpr, hash, in includeValues, in provider),
                MethodCallExpression methodExpr => MethodCallExpression(in methodExpr, hash, in includeValues, in provider),
                LambdaExpression lambdaExpr => LambdaExpression(in lambdaExpr, hash, in includeValues, in provider),
                NewArrayExpression newArrayExpression => HandleNewArrayExpression(in newArrayExpression, in hash, includeValues, provider),
                ParameterExpression parameterExpression => hash * 397 + parameterExpression.Name.GetHashCode(),
                NewExpression newExpression => HandleNewExpression(in newExpression, in hash, in includeValues),
                _ => throw new NotImplementedException(),
            };
        }

        private static int HandleNewArrayExpression(in NewArrayExpression mainExpression, in int hash, bool includeValues, DbProvider provider)
        {
            return mainExpression.Expressions.Aggregate(hash, (current, expression) => GetHashCode(in expression, in current, includeValues, provider));
        }

        private static int HandleNewExpression(in NewExpression newExpression, in int hash, in bool includeValues)
        {
            if (newExpression.Type == typeof(DateTime))
            {
                if (includeValues)
                {
                    var instantiator = Expression.Lambda<Func<DateTime>>(newExpression).Compile();
                    var value = instantiator();
                    return (hash * 23) + value.GetHashCode();
                }

                return (hash * 23) + newExpression.Type.GetHashCode();
            }

            return hash;
        }

        private static int ConstantExpression(in ConstantExpression constExpr, in MemberInfo member, int hash, in bool includeValues)
        {
            if (includeValues)
                return (hash * 23) + constExpr.Type.GetHashCode();

            hash = member != null && member is FieldInfo fieldInfo
                ? (hash * 23) + fieldInfo.Name.GetHashCode()
                : (hash * 23) + constExpr.Type.GetHashCode();

            return hash;
        }

        private static int BinaryExpressionHash(in BinaryExpression binExpr, int hash, in bool includeValues, in DbProvider provider)
        {
            var tempHash = GetHashCode(binExpr.Left, hash, includeValues, provider);
            hash = (hash * 23) + tempHash;
            tempHash = GetHashCode(binExpr.Right, hash, includeValues, provider);
            return (hash * 23) + tempHash;
        }

        public static int MemberExpression(in MemberExpression memberExpression, int hash, in bool includeValues, in DbProvider provider)
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

        public static int MethodCallExpression(in MethodCallExpression methodExpr, int hash, in bool includeValues, in DbProvider provider)
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

        public static int LambdaExpression(in LambdaExpression lambdaExpr, int hash, in bool includeValues, in DbProvider provider)
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
