using System;
using System.Linq;
using System.Linq.Expressions;

namespace Thomas.Database.Helpers
{
    internal class ExpressionHasher
    {
        public static ulong GetHashCode<T>(Expression<Func<T, bool>> expr, SqlProvider provider, bool includeValues = true)
        {
            ulong hash = (ulong)expr.Body.NodeType.GetHashCode();
            hash = hash * 397 ^ HashHelper.GenerateHash(provider.ToString());
            return GetHashCode(expr.Body, hash, includeValues, provider);
        }

        private static ulong GetHashCode(Expression expr, ulong hash, bool includeValues, SqlProvider provider)
        {
            return expr switch
            {
                UnaryExpression unaryExpr => hash * 397 ^ GetHashCode(unaryExpr.Operand, hash, includeValues, provider),
                BinaryExpression binExpr => BinaryExpressionHash(binExpr, hash, includeValues, provider),
                ConstantExpression constExpr => ConstantExpression(constExpr, hash, includeValues),
                MemberExpression memberExpr => MemberExpression(memberExpr, hash, includeValues, provider),
                MethodCallExpression methodExpr => MethodCallExpression(methodExpr, hash, includeValues, provider),
                LambdaExpression lambdaExpr => LambdaExpression(lambdaExpr, hash, includeValues, provider),
                NewArrayExpression newArrayExpression => newArrayExpression.Expressions.Aggregate(hash, (current, expression) => GetHashCode(expression, current, includeValues, provider)),
                ParameterExpression parameterExpression => hash * 397 ^ HashHelper.GenerateHash(parameterExpression.Name),
                NewExpression newExpression => HandleNewExpression(newExpression, hash, includeValues),
                _ => throw new NotImplementedException(),
            };
        }

        private static ulong HandleNewExpression(NewExpression newExpression, ulong hash, bool includeValues)
        {
            if (newExpression.Type == typeof(DateTime))
            {
                if (includeValues)
                {
                    var instantiator = Expression.Lambda<Func<DateTime>>(newExpression).Compile();
                    var value = instantiator();

                    return hash * 397 ^ (ulong)value.Ticks;
                }
                else
                {
                    return hash * 397 ^ HashHelper.GenerateHash("DateTime");
                }
            }

            return hash;
        }

        private static ulong ConstantExpression(ConstantExpression constExpr, ulong hash, bool includeValues)
        {
            if (includeValues)
                return hash * 397 ^ HashHelper.GenerateHash((constExpr.Type.FullName ?? "").ToString());

            return hash * 397 ^ HashHelper.GenerateHash((constExpr.Value ?? "").ToString());
        }

        private static ulong BinaryExpressionHash(BinaryExpression binExpr, ulong hash, bool includeValues, SqlProvider provider)
        {
            var tempHash = GetHashCode(binExpr.Left, hash, includeValues, provider);
            hash = hash * 397 ^ tempHash;
            tempHash = GetHashCode(binExpr.Right, hash, includeValues, provider);
            hash = hash * 397 ^ tempHash;
            return hash;
        }

        public static ulong MemberExpression(MemberExpression memberExpr, ulong hash, bool includeValues, SqlProvider provider)
        {
            hash = hash * 397 ^ HashHelper.GenerateHash(memberExpr.Member.Name);

            if (memberExpr.Expression != null)
                hash = hash * 397 ^ GetHashCode(memberExpr.Expression, hash, includeValues, provider);

            return hash;
        }

        public static ulong MethodCallExpression(MethodCallExpression methodExpr, ulong hash, bool includeValues, SqlProvider provider)
        {
            hash = hash * 397 ^ HashHelper.GenerateHash(methodExpr.Method.Name);
            foreach (var arg in methodExpr.Arguments)
            {
                if (arg is ConstantExpression expression)
                    hash = hash * 397 ^ HashHelper.GenerateHash(provider.ToString());
                else
                    hash = hash * 397 ^ GetHashCode(arg, hash, includeValues, provider);
            }
            return hash;
        }

        public static ulong LambdaExpression(LambdaExpression lambdaExpr, ulong hash, bool includeValues, SqlProvider provider)
        {
            hash = hash * 397 ^ GetHashCode(lambdaExpr.Body, hash, includeValues, provider);
            foreach (var p in lambdaExpr.Parameters)
            {
                hash = hash * 397 ^ HashHelper.GenerateHash(p.Name);
            }
            return hash;
        }

    }
}
