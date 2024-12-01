using System;

namespace TData
{
    [Flags]
    internal enum MethodHandled
    {
        Execute = 0,
        FetchListExpression = 1 << 0,
        FetchListQueryString = 1 << 1,
        FetchOneExpression = 1 << 2,
        FetchOneQueryString = 1 << 3,
        FetchTupleQueryString_2 = 1 << 4,
        FetchTupleQueryString_3 = 1 << 5,
        FetchTupleQueryString_4 = 1 << 6,
        FetchTupleQueryString_5 = 1 << 7,
        FetchTupleQueryString_6 = 1 << 8,
        FetchTupleQueryString_7 = 1 << 9,
    }
}
