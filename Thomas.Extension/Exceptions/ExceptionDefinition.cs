using System;

namespace Thomas.Cache.Exceptions
{
    public sealed class NullValueCachedException : Exception
    {
        public NullValueCachedException(string message) : base(message) { }
    }

    public sealed class DbTypeNotFoundException : Exception
    {
        public DbTypeNotFoundException(string message) : base(message) { }
    }
}
