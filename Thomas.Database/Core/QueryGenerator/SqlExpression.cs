using System;

namespace Thomas.Database.Core.QueryGenerator
{
    public class SqlExpression
    {
        public static bool Between<T>(Func<T, object> field, int a, int b) => true;
        public static bool Between<T>(Func<T, object> field, long a, long b) => true;
        public static bool Between<T>(Func<T, object> field, decimal a, decimal b) => true;
        public static bool Between<T>(Func<T, object> field, short a, short b) => true;
        public static bool Between<T>(Func<T, object> field, float a, float b) => true;
        public static bool Between<T>(Func<T, object> field, double a, double b) => true;
        public static bool Between<T>(Func<T, object> field, DateTime a, DateTime b) => true;
        public static bool Exists<TPrincipal, TSubquery>(Func<TPrincipal, TSubquery, object> field) => true;
    }
}
