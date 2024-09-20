using System;

namespace Thomas.Database
{
    public sealed class DuplicateSignatureException : Exception { }

    public sealed class DBProviderNotFoundException : Exception {
        public DBProviderNotFoundException(string message) : base(message) { }
    }

    public sealed class PostgreSQLInvalidRequestCallException : Exception {

        public override string Message => "PostgreSQL function call is invalid because cannot call a function that return a result set and contains output parameters.\r\nInstead of that return two result sets, one with the result set and another with the output parameters.";
    }

    public sealed class ScriptNotProvidedException : Exception { }
    public sealed class MissingParametersException : Exception { }
    public sealed class NotAllowParametersException : Exception { }
    public sealed class UnsupportedParametersException : Exception { }

    public sealed class NotSupportedCommandTypeException : Exception { }

    public sealed class PropertiesNotFoundException : Exception {
        public PropertiesNotFoundException(string message) : base(message) { }
    }

    public sealed class DbNullToValueTypeException : Exception {
        public DbNullToValueTypeException(string message) : base(message) { }
    }

    public sealed class TimeSpanConversionException : Exception
    {
        public TimeSpanConversionException(string message) : base(message) { }
    }

    public sealed class RequestNotPermittedException : Exception
    {
        public RequestNotPermittedException(string message) : base(message) { }
    }
}
