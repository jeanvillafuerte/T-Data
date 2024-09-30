using System.Runtime.CompilerServices;

namespace TData
{
    public enum MethodHandled : byte
    {
        Execute = 0,
        FetchListExpression = 1,
        FetchListQueryString = 2,
        FetchOneExpression = 3,
        FetchOneQueryString = 4,
        FetchTupleQueryString_2 = 5,
        FetchTupleQueryString_3 = 6,
        FetchTupleQueryString_4 = 7,
        FetchTupleQueryString_5 = 8,
        FetchTupleQueryString_6 = 9,
        FetchTupleQueryString_7 = 10,
    }
}
