using System;

namespace Thomas.Cache.Exceptions
{
    public sealed class NotNullValueAllowedException : Exception
    {
        public NotNullValueAllowedException(string message) : base(message) { }
    }

    public sealed class DbTypeNotFoundException : Exception
    {
        public DbTypeNotFoundException(string message) : base(message) { }
    }
}
