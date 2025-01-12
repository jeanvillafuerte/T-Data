using System;

namespace TData
{
    [Flags]
    public enum DbProvider : byte
    {
        SqlServer = 0,
        MySql = 1 << 0,
        PostgreSql = 1 << 1,
        Oracle = 1 << 2,
        Sqlite = 1 << 3
    }
}
